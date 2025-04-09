using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProyectoLFP
{
    /// <summary>
    /// Lógica de interacción para Window1.xaml
    /// </summary>
    public partial class ReporteTokensWindow : Window
    {
        private List<TokenReporte> listaOriginal;

        public ReporteTokensWindow(List<(string Token, string Lexema, int Linea, int Columna)> tokens)
        {
            InitializeComponent();
            listaOriginal = FormatearDatos(tokens); 
            dataGridTokens.ItemsSource = listaOriginal;

        }

        private List<TokenReporte> FormatearDatos(List<(string Token, string Lexema, int Linea, int Columna)> tokens)
        {
            var lista = new List<TokenReporte>();
            foreach (var (token, lexema, linea, columna) in tokens)
            {
                lista.Add(new TokenReporte
                {
                    Token = token,
                    Lexema = lexema,
                    Linea = linea,
                    Columna = columna
                });
            }
            return lista;
        }
        private void txtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string textoBuscar = txtBuscar.Text.ToLower();

            var filtrados = listaOriginal.Where(t =>
                t.Token.ToLower().Contains(textoBuscar) ||
                t.Lexema.ToLower().Contains(textoBuscar) ||
                t.Linea.ToString().Contains(textoBuscar) ||
                t.Columna.ToString().Contains(textoBuscar)
            ).ToList();

            dataGridTokens.ItemsSource = filtrados;

            txtCoincidencias.Text = $"Coincidencias: {filtrados.Count}";
        }
        public class TokenReporte
        {
            public string Token { get; set; }
            public string Lexema { get; set; }
            public int Linea { get; set; }
            public int Columna { get; set; }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Archivo de texto (*.txt)|*.txt";
            saveFileDialog.FileName = "ReporteTokens";

            if (saveFileDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Token,Lexema,Línea,Columna");

                foreach (TokenReporte token in dataGridTokens.ItemsSource)
                {
                    sb.AppendLine($"{token.Token},{token.Lexema},{token.Linea},{token.Columna}");
                }

                System.IO.File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Reporte exportado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


    }
}


