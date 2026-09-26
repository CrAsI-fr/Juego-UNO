using MySqlConnector;
using System.Collections.Generic;

namespace Juego_UNO
{
    /// <summary>
    /// Acceso a la base de datos unobd.
    /// </summary>
    public static class BaseDatos
    {
        // Cambiar usuario y contraseña segun la instalacion de MySQL de cada quien
        private const string mypass = "SQLito";
        //hacer que reconozca mypass como variable de entorno para no tener que poner la contraseña en el código
        private const string CadenaConexion = "Server=localhost;Port=3306;Database=unobd;User ID=root;Password=SQLito;";

        private static MySqlConnection AbrirConexion()
        {
            var conexion = new MySqlConnection(CadenaConexion);
            conexion.Open();
            return conexion;
        }

        /// <summary>
        /// Busca al jugador por nombre; si no existe lo da de alta.
        /// </summary>
        public static Jugador ObtenerOCrearJugador(string nombre)
        {
            using (var conexion = AbrirConexion())
            {
                using (var comando = new MySqlCommand(
                    "select id_jugador from jugador where nombre_jug = @nombre", conexion))
                {
                    comando.Parameters.AddWithValue("@nombre", nombre);
                    object id = comando.ExecuteScalar();
                    if (id != null)
                        return new Jugador { Id = System.Convert.ToInt32(id), Nombre = nombre };
                }

                using (var comando = new MySqlCommand(
                    "insert into jugador (nombre_jug) values (@nombre)", conexion))
                {
                    comando.Parameters.AddWithValue("@nombre", nombre);
                    comando.ExecuteNonQuery();
                    return new Jugador { Id = (int)comando.LastInsertedId, Nombre = nombre };
                }
            }
        }

        /// <summary>
        /// Devuelve todas las cartas del mazo.
        /// </summary>
        public static List<Carta> ObtenerCartas()
        {
            var cartas = new List<Carta>();
            using (var conexion = AbrirConexion())
            using (var comando = new MySqlCommand(
                "select id_carta, tipo_carta, color_carta, efecto, num_carta from carta", conexion))
            using (var lector = comando.ExecuteReader())
            {
                while (lector.Read())
                {
                    cartas.Add(new Carta
                    {
                        Id = lector.GetInt32(0),
                        Tipo = lector.GetString(1),
                        Color = lector.IsDBNull(2) ? null : lector.GetString(2),
                        Efecto = lector.IsDBNull(3) ? null : lector.GetString(3),
                        Numero = lector.IsDBNull(4) ? (int?)null : lector.GetInt32(4)
                    });
                }
            }
            return cartas;
        }

        /// <summary>
        /// Crea la partida, registra a los jugadores que participan y guarda en el log
        /// las cartas repartidas a cada uno. Todo en una sola transaccion.
        /// Devuelve el id de la partida creada.
        /// </summary>
        public static int RegistrarPartida(List<Jugador> jugadores)
        {
            using (var conexion = AbrirConexion())
            using (var transaccion = conexion.BeginTransaction())
            {
                int idPartida;
                using (var comando = new MySqlCommand(
                    "insert into partida (fecha_inicio) values (now())", conexion, transaccion))
                {
                    comando.ExecuteNonQuery();
                    idPartida = (int)comando.LastInsertedId;
                }

                using (var comando = new MySqlCommand(
                    "insert into partida_jugador (id_partida, id_jugador, posicion) " +
                    "values (@partida, @jugador, @posicion)", conexion, transaccion))
                {
                    comando.Parameters.AddWithValue("@partida", idPartida);
                    var pJugador = comando.Parameters.AddWithValue("@jugador", 0);
                    var pPosicion = comando.Parameters.AddWithValue("@posicion", 0);

                    for (int i = 0; i < jugadores.Count; i++)
                    {
                        pJugador.Value = jugadores[i].Id;
                        pPosicion.Value = i + 1;
                        comando.ExecuteNonQuery();
                    }
                }

                using (var comando = new MySqlCommand(
                    "insert into jugada (id_partida, num_jugada, id_jugador, tipo_accion, id_carta) " +
                    "values (@partida, @num, @jugador, 'repartir', @carta)", conexion, transaccion))
                {
                    comando.Parameters.AddWithValue("@partida", idPartida);
                    var pNum = comando.Parameters.AddWithValue("@num", 0);
                    var pJugador = comando.Parameters.AddWithValue("@jugador", 0);
                    var pCarta = comando.Parameters.AddWithValue("@carta", 0);

                    // Se registra en el mismo orden en que se reparte: una carta por jugador por vuelta
                    int numJugada = 1;
                    for (int vuelta = 0; vuelta < jugadores[0].Mano.Count; vuelta++)
                    {
                        foreach (var jugador in jugadores)
                        {
                            pNum.Value = numJugada++;
                            pJugador.Value = jugador.Id;
                            pCarta.Value = jugador.Mano[vuelta].Id;
                            comando.ExecuteNonQuery();
                        }
                    }
                }

                transaccion.Commit();
                return idPartida;
            }
        }
    }
}
