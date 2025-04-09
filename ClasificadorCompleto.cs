using System.Collections.Generic;
using System.Text;

namespace ProyectoLFP
{
    internal class ClasificadorCompleto
    {
        private static readonly HashSet<string> palabrasReservadas = new HashSet<string>
        {
            "if", "class", "for", "then", "else", "public", "private", "package", "import", "static", "void",
            "int", "true", "false", "extends", "short", "boolean", "float", "interface", "final", "protected",
            "return", "while", "case", "implements"
        };

        private static readonly HashSet<string> operadoresLogicos = new HashSet<string> { "AND", "OR"};


        private Dictionary<(string estado, string tipoCaracter), string> transiciones;

        public ClasificadorCompleto()
        {
            InicializarTransiciones();
        }

        private void InicializarTransiciones()
        {
            transiciones = new Dictionary<(string, string), string>();

            // Número Entero
            transiciones[("Inicio", "signo")] = "SignoNumero";
            transiciones[("Inicio", "cero")] = "Cero";
            transiciones[("Inicio", "digitoNoCero")] = "Entero";
            transiciones[("SignoNumero", "cero")] = "Cero";
            transiciones[("SignoNumero", "digitoNoCero")] = "Entero";
            transiciones[("Entero", "digito")] = "Entero";
            transiciones[("Entero", "punto")] = "DecimalPunto";

            // Decimal
            transiciones[("DecimalPunto", "digito")] = "Decimal";
            transiciones[("Decimal", "digito")] = "Decimal";

            // Identificador
            transiciones[("Inicio", "dolar")] = "IdentificadorInicio";
            transiciones[("IdentificadorInicio", "idcaracter")] = "Identificador";
            transiciones[("Identificador", "idcaracter")] = "Identificador";

            // Palabra Normal (sin $)
            transiciones[("Inicio", "letra")] = "Palabra";
            transiciones[("Palabra", "letra")] = "Palabra";
            transiciones[("Palabra", "digito")] = "Palabra";

            // Literal
            transiciones[("Inicio", "comillaDoble")] = "LiteralDobleInicio";
            transiciones[("Inicio", "comillaSimple")] = "LiteralSimpleInicio";

            transiciones[("LiteralDobleInicio", "espacio")] = "LiteralDobleInicio";
            transiciones[("LiteralDobleInicio", "noComillaDoble")] = "LiteralDobleInicio";
            transiciones[("LiteralDobleInicio", "comillaDoble")] = "LiteralDobleFin";

            transiciones[("LiteralSimpleInicio", "espacio")] = "LiteralSimpleInicio";
            transiciones[("LiteralSimpleInicio", "noComillaSimple")] = "LiteralSimpleInicio";
            transiciones[("LiteralSimpleInicio", "comillaSimple")] = "LiteralSimpleFin";

            // Comentario de una línea
            transiciones[("Inicio", "gato")] = "ComentarioLinea";
            transiciones[("ComentarioLinea", "noSalto")] = "ComentarioLinea";

            // Comentario de bloque
            transiciones[("Inicio", "barra")] = "PosibleComentarioBloqueInicio";
            transiciones[("PosibleComentarioBloqueInicio", "asterisco")] = "ComentarioBloque";
            transiciones[("ComentarioBloque", "otro")] = "ComentarioBloque";
            transiciones[("ComentarioBloque", "asterisco")] = "ComentarioBloqueAsterisco";
            transiciones[("ComentarioBloqueAsterisco", "barra")] = "ComentarioBloqueFin";
            transiciones[("ComentarioBloqueAsterisco", "otro")] = "ComentarioBloque";

            // Operadores y símbolos
            transiciones[("Inicio", "operadorAritmetico")] = "OperadorAritmetico";
            transiciones[("Inicio", "igual")] = "Asignacion";
            transiciones[("Inicio", "signoAgrupacion")] = "SignoAgrupacion";
            transiciones[("Inicio", "signoPuntuacion")] = "SignoPuntuacion";

            transiciones[("Inicio", "operador")] = "PosibleOperadorLogico";
            transiciones[("PosibleOperadorLogico", "operador")] = "OperadorLogico";

            transiciones[("Inicio", "operadorR")] = "OperadorRelacional";
            transiciones[("OperadorRelacional", "igual")] = "OperadorRelacional";

        }

        private string ClasificarCaracter(char c)
        {
            if (c == '+' || c == '-' || c == '*' || c == '/' || c == '^') return "operadorAritmetico";
            if (c == '0') return "cero";
            if (c >= '1' && c <= '9') return "digitoNoCero";
            if (c >= '0' && c <= '9') return "digito";
            if (c == '$') return "dolar";
            if (char.IsLetter(c)) return "letra";
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-') return "idcaracter";
            if (c == '"' || c == '“' || c == '”') return "comillaDoble";
            if (c == '\'' || c == '‘' || c == '’') return "comillaSimple";
            if (c == '#') return "gato";
            if (c == '/') return "barra";
            if (c == '*') return "asterisco";
            if (c == '\n') return "salto";
            if (c == '=') return "igual";
            if (c == '&' || c == '|') return "operador";
            if (c == '>' || c == '<') return "operadorR";
            if ("{}[]()".Contains(c)) return "signoAgrupacion";
            if (".,;:".Contains(c)) return "signoPuntuacion";
            if (c == ' ') return "espacio";
            return "otro";
        }


        public string ClasificarEntrada(string entrada)
        {
            entrada = entrada.Trim();
            if (entrada == string.Empty)
                return "Entrada inválida";

            if (entrada.StartsWith("/*") && entrada.EndsWith("*/"))
                return "Comentario en bloque";

            if (entrada.StartsWith("#"))
                return "Comentario de una sola línea";
            
            if ((entrada.StartsWith("“") && entrada.EndsWith("”")) || (entrada.StartsWith("\"") && entrada.EndsWith("\"")) || (entrada.StartsWith("'") && entrada.EndsWith("‘")) || (entrada.StartsWith("’") && entrada.EndsWith("'")))
                return "Literal";
            if ((entrada.StartsWith("$")))
                return "Identificador";


            string estado = "Inicio";
            int pos = 0;

            while (pos < entrada.Length)
            {
                char actual = entrada[pos];
                string tipo = ClasificarCaracter(actual);

                if (estado == "ComentarioLinea" && tipo == "salto")
                {
                    estado = "ComentarioLineaFin";
                    pos++;
                    break;
                }

                if (estado == "ComentarioBloque" && tipo == "asterisco")
                    estado = "ComentarioBloqueAsterisco";
                else if (estado == "ComentarioBloqueAsterisco")
                {
                    if (tipo == "barra")
                    {
                        estado = "ComentarioBloqueFin";
                        pos++;
                        break;
                    }
                    else if (tipo != "asterisco")
                        estado = "ComentarioBloque";
                }

                if (estado.StartsWith("LiteralDobleInicio"))
                {
                    if (tipo == "comillaDoble")
                    {
                        estado = "LiteralDobleFin";
                        pos++;
                        break;
                    }
                    if (tipo == "salto")
                        return "Error: Literal sin cerrar";
                }

                if (estado.StartsWith("LiteralSimpleInicio"))
                {
                    if (tipo == "comillaSimple")
                    {
                        estado = "LiteralSimpleFin";
                        pos++;
                        break;
                    }
                    if (tipo == "salto")
                        return "Error: Literal sin cerrar";
                }

                if (transiciones.TryGetValue((estado, tipo), out string nuevoEstado))
                    estado = nuevoEstado;
                else
                    break;

                pos++;
            }

            if (estado == "Entero" || estado == "Cero")
                return "Número entero";
            if (estado == "Decimal")
                return "Número decimal";
            if (estado == "Identificador")
                return "Identificador";
            if (estado == "Palabra")
            {
                string palabra = entrada;
                if (operadoresLogicos.Contains(palabra))
                    return "Operador lógico";
                if (palabrasReservadas.Contains(palabra))
                    return "Palabra reservada";
                return "Cadena";
            }
            if (estado == "LiteralDobleFin" || estado == "LiteralSimpleFin")
                return "Literal";
            if (estado == "ComentarioLineaFin")
                return "Comentario de una sola línea";
            if (estado == "ComentarioBloqueFin")
                return "Comentario en bloque";
            if (estado == "OperadorAritmetico")
                return "Operador Aritmetico";
            if (estado == "Asignacion")
                return "Operador de asignación";
            if (estado == "SignoAgrupacion")
                return "Signo de agrupación";
            if (estado == "SignoPuntuacion")
                return "Signo de puntuación";
            if (estado == "OperadorLogico")
                return "Operador lógico";
            if (estado == "OperadorRelacional")
                return "Operador relacional";
            if (estado == "ErrorLiteral")
                return "Error: Literal sin cerrar";

            return "Entrada inválida";

        }
    }
}