using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProyectoLFP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string archivoActual = null;
        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void EditorTexto_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextPointer caret = EditorTexto.CaretPosition;
            TextRange rango = new TextRange(EditorTexto.Document.ContentStart, caret);
            string textoHastaCursor = rango.Text;

            int linea = 1;
            int columna = 1;

            if(!string.IsNullOrEmpty(textoHastaCursor))
            {
                string[] lineas = textoHastaCursor.Split(new[] { '\n' }, StringSplitOptions.None);
                linea = lineas.Length;
                columna = lineas[lineas.Length - 1].Length + 1;
            }

            IndicadorLineaColumna.Content = $"Línea: {linea}, Columna: {columna}";
        }
        private void Nuevo_Click(object sender, RoutedEventArgs e)
        {
            EditorTexto.Document.Blocks.Clear();
            archivoActual = null;
        }

        private void AbrirArchivo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                using (StreamReader reader = new StreamReader(openFileDialog.FileName))
                {
                    EditorTexto.Document.Blocks.Clear();
                    EditorTexto.AppendText(reader.ReadToEnd());
                }
                archivoActual = openFileDialog.FileName;
            }
        }


        private void GuardarArchivo_Click(object sender, RoutedEventArgs e)
        {
            if (archivoActual == null)
            {
                GuardarComo_Click(sender, e);
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(archivoActual))
                {
                    writer.Write(new TextRange(EditorTexto.Document.ContentStart, EditorTexto.Document.ContentEnd).Text);
                }
            }
        }

        private void GuardarComo_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                archivoActual = saveFileDialog.FileName;
                using (StreamWriter writer = new StreamWriter(archivoActual))

                {
                    writer.Write(new TextRange(EditorTexto.Document.ContentStart, EditorTexto.Document.ContentEnd).Text);
                }
            }
        }

        private void Buscar_Click(object sender, RoutedEventArgs e)
        {
            string cadena = EntradaBusqueda.Text;
            if (!string.IsNullOrEmpty(cadena))
            {
                BuscarYResaltar(cadena);
            }
            else
            {
                MessageBox.Show("Por favor ingrese una cadena para buscar.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BuscarYResaltar(string cadena)
        {
            int coincidencias = 0;

            TextRange textoCompleto = new TextRange(EditorTexto.Document.ContentStart, EditorTexto.Document.ContentEnd);
            textoCompleto.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);

            TextPointer inicio = EditorTexto.Document.ContentStart;

            while (inicio != null && inicio.CompareTo(EditorTexto.Document.ContentEnd) < 0)
            {
                TextRange palabraEncontrada = BuscarEnTexto(inicio, EditorTexto.Document.ContentEnd, cadena);
                if (palabraEncontrada == null)
                    break;

                palabraEncontrada.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
                coincidencias++;

                inicio = palabraEncontrada.End;
            }

            EtiquetaCoincidencias.Content = $"No. Repeticiones: {coincidencias}";
        }
        private TextRange BuscarEnTexto(TextPointer inicio, TextPointer fin, string texto)
        {
            while (inicio != null && inicio.CompareTo(fin) < 0)
            {
                if (inicio.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textoEnRun = inicio.GetTextInRun(LogicalDirection.Forward);

                    int index = textoEnRun.IndexOf(texto, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        TextPointer inicioPalabra = inicio.GetPositionAtOffset(index);
                        TextPointer finPalabra = inicioPalabra.GetPositionAtOffset(texto.Length);

                        return new TextRange(inicioPalabra, finPalabra);
                    }
                }
                inicio = inicio.GetNextContextPosition(LogicalDirection.Forward);
            }
            return null;
        }

        private void AnalizarTexto_Click(object sender, RoutedEventArgs e)
        {
            var clasificador = new ClasificadorCompleto();
            var errores = new List<string>();
            var reporteTokens = new List<(string Token, string Lexema, int Linea, int Columna)>();

            string texto = new TextRange(EditorTexto.Document.ContentStart, EditorTexto.Document.ContentEnd).Text;
            int fila = 1;
            int columna = 1;
            int inicioColumna = 1;

            bool enLiteralDoble = false;
            bool enLiteralSimple = false;
            bool enComentarioLinea = false;
            bool enComentarioBloque = false;

            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < texto.Length; i++)
            {
                char actual = texto[i];
                char siguiente = (i + 1 < texto.Length) ? texto[i + 1] : '\0';

                if (enLiteralDoble)
                {
                    buffer.Append(actual);
                    if (actual == '"' && buffer.Length > 1)
                    {
                        string lexema = buffer.ToString();
                        string resultado = clasificador.ClasificarEntrada(lexema);
                        reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                        buffer.Clear();
                        enLiteralDoble = false;
                    }
                    else if (actual == '\n')
                    {
                        errores.Add($"Error: Literal de comillas dobles sin cerrar en línea {fila}.");
                        enLiteralDoble = false;
                        buffer.Clear();
                    }
                    if (actual == '\n') { fila++; columna = 0; }
                    columna++;
                    continue;
                }
                if (enLiteralSimple)
                {
                    buffer.Append(actual);
                    if (actual == '\'' && buffer.Length > 1)
                    {
                        string lexema = buffer.ToString();
                        string resultado = clasificador.ClasificarEntrada(lexema);
                        reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                        buffer.Clear();
                        enLiteralSimple = false;
                    }
                    else if (actual == '\n')
                    {
                        errores.Add($"Error: Literal de comillas simples sin cerrar en línea {fila}.");
                        enLiteralSimple = false;
                        buffer.Clear();
                    }
                    if (actual == '\n') { fila++; columna = 0; }
                    columna++;
                    continue;
                }
                if (enComentarioLinea)
                {
                    buffer.Append(actual);
                    if (actual == '\n')
                    {
                        string lexema = buffer.ToString().TrimEnd('\n', '\r');
                        string resultado = clasificador.ClasificarEntrada(lexema);
                        reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                        buffer.Clear();
                        enComentarioLinea = false;
                        fila++;
                        columna = 0;
                    }
                    else
                    {
                        columna++;
                    }
                    continue;
                }
                if (enComentarioBloque)
                {
                    buffer.Append(actual);
                    if (actual == '*' && siguiente == '/')
                    {
                        buffer.Append('/');
                        i++;
                        string lexema = buffer.ToString();
                        string resultado = clasificador.ClasificarEntrada(lexema);
                        reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                        buffer.Clear();
                        enComentarioBloque = false;
                        columna += 2;
                        continue;
                    }
                    if (actual == '\n') { fila++; columna = 0; }
                    else { columna++; }
                    continue;
                }

                if (actual == '"' && !enLiteralDoble)
                {
                    enLiteralDoble = true;
                    buffer.Append(actual);
                    inicioColumna = columna;
                    columna++;
                    continue;
                }
                if (actual == '\'' && !enLiteralSimple)
                {
                    enLiteralSimple = true;
                    buffer.Append(actual);
                    inicioColumna = columna;
                    columna++;
                    continue;
                }
                if (actual == '#' && !enComentarioLinea)
                {
                    enComentarioLinea = true;
                    buffer.Append(actual);
                    inicioColumna = columna;
                    columna++;
                    continue;
                }
                if (actual == '/' && siguiente == '*' && !enComentarioBloque)
                {
                    enComentarioBloque = true;
                    buffer.Append(actual);
                    buffer.Append(siguiente);
                    i++;
                    inicioColumna = columna;
                    columna += 2;
                    continue;
                }

                if ((actual == '&' && siguiente == '&') ||
                    (actual == '|' && siguiente == '|') ||
                    (actual == '<' && siguiente == '=') ||
                    (actual == '>' && siguiente == '=') ||
                    (actual == '=' && siguiente == '=') ||
                    (actual == '!' && siguiente == '='))
                { 
                    if (buffer.Length > 0)
                    {
                        string lexema = buffer.ToString();
                        string resultado = clasificador.ClasificarEntrada(lexema);

                        if (resultado == "Entrada inválida")
                        {
                            errores.Add($"Error en línea {fila}, columna {inicioColumna}: Token inválido '{lexema}'");
                        }
                        else
                        {
                            reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                        }
                        buffer.Clear();
                    }

                    string operadorDoble = "" + actual + siguiente;
                    string resultadoDoble = clasificador.ClasificarEntrada(operadorDoble);

                    if (resultadoDoble == "Entrada inválida")
                    {
                        errores.Add($"Error en línea {fila}, columna {columna}: Token inválido '{operadorDoble}'");
                    }
                    else
                    {
                        reporteTokens.Add((resultadoDoble, operadorDoble, fila, columna));
                    }

                    i++;
                    columna += 2;
                    inicioColumna = columna;
                    continue;
                }

                if (char.IsWhiteSpace(actual) || EsSeparador(actual))
                {
                    if (buffer.Length > 0)
                    {
                        string lexema = buffer.ToString();
                        string resultado = clasificador.ClasificarEntrada(lexema);

                        if (resultado == "Entrada inválida")
                        {
                            errores.Add($"Error en línea {fila}, columna {inicioColumna}: Token inválido '{lexema}'");
                        }
                        else
                        {
                            reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                        }
                        buffer.Clear();
                    }

                    if (EsSeparador(actual))
                    {
                        string separador = actual.ToString();
                        string resultado = clasificador.ClasificarEntrada(separador);

                        if (resultado == "Entrada inválida")
                        {
                            errores.Add($"Error en línea {fila}, columna {columna}: Token inválido '{separador}'");
                        }
                        else
                        {
                            reporteTokens.Add((resultado, separador, fila, columna));
                        }
                    }

                    if (actual == '\n')
                    {
                        fila++;
                        columna = 0;
                    }

                    inicioColumna = columna + 1;
                }
                else
                {
                    if (buffer.Length == 0)
                        inicioColumna = columna;

                    buffer.Append(actual);
                }

                columna++;
            }

            if (buffer.Length > 0)
            {
                string lexema = buffer.ToString();
                string resultado = clasificador.ClasificarEntrada(lexema);

                if (resultado == "Entrada inválida")
                {
                    errores.Add($"Error en línea {fila}, columna {inicioColumna}: Token inválido '{lexema}'");
                }
                else
                {
                    reporteTokens.Add((resultado, lexema, fila, inicioColumna));
                }
            }

            if (errores.Count > 0)
            {
                MostrarErrores(errores);
            }
            else
            {
                MostrarReporteTokens(reporteTokens);
            }
        }

        private bool EsSeparador(char c)
        {
            return "+-*/^=(){}[],;:<>!&|\"'".Contains(c);
        }

        private void Copiar_Click(object sender, RoutedEventArgs e)
        {
            TextSelection seleccion = EditorTexto.Selection;

            if (!seleccion.IsEmpty)
            {
                Clipboard.SetText(seleccion.Text); 
            }
            else
            {
                MessageBox.Show("No hay texto seleccionado para copiar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Pegar_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                EditorTexto.Selection.Text = Clipboard.GetText(); 
            }
            else
            {
                MessageBox.Show("El portapapeles no contiene texto válido.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Deshacer_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTexto.CanUndo)
            {
                EditorTexto.Undo();
            }
            else
            {
                MessageBox.Show("No hay acciones para deshacer.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void Rehacer_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTexto.CanRedo)
            {
                EditorTexto.Redo();
            }
            else
            {
                MessageBox.Show("No hay acciones para rehacer.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        

        public class ErrorLexico
        {
            public int Linea { get; set; }
            public int Columna { get; set; }
            public string Mensaje { get; set; }
        }


        
        private void MostrarErrores(List<string> errores)
        {
            if (errores.Count > 0)
            {
                var listaErrores = new List<ErrorLexico>();

                foreach (string error in errores)
                {
                    string[] partes = error.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length > 6 && int.TryParse(partes[3].TrimEnd(','), out int linea) && int.TryParse(partes[5].TrimEnd(':'), out int columna))
                    {
                        string mensaje = string.Join(" ", partes.Skip(6));
                        listaErrores.Add(new ErrorLexico { Linea = linea, Columna = columna, Mensaje = mensaje });
                    }
                    else
                    {
                    }
                }

                TablaErrores.ItemsSource = listaErrores;
                TablaErrores.Visibility = Visibility.Visible;
            }
            else
            {
                TablaErrores.Visibility = Visibility.Collapsed;
            }
        }

        private void MostrarReporteTokens(List<(string Token, string Lexema, int Linea, int Columna)> reporteTokens)
        {
            var ventana = new ReporteTokensWindow(reporteTokens);
            ventana.ShowDialog(); 
            
        }

        private void AcercaDe_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Aplicación desarrollada por: Dayana Sofía Orozco Mendóza\nEstudiante de: Ingenieria en Ciencias y Sistemas", "Acerca de");
        }
    }
}