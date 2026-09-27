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
        private const string CadenaConexion = "Server=localhost;Port=3306;Database=unobd;User ID=root;Password=Darkneznight1987*;";

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
        /// las cartas repartidas a cada uno y la carta inicial. Todo en una sola transaccion.
        /// Devuelve el id de la partida creada.
        /// </summary>
        public static int RegistrarPartida(List<Jugador> jugadores, Carta cartaInicial)
        {
            using (var conexion = AbrirConexion())
            using (var transaccion = conexion.BeginTransaction())
            {
                int idPartida;
                int numJugada;
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
                    numJugada = 1;
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

                using (var comando = new MySqlCommand(
                    "insert into jugada (id_partida, num_jugada, id_jugador, tipo_accion, id_carta) " +
                    "values (@partida, @num, null, 'carta_inicial', @carta)", conexion, transaccion))
                {
                    comando.Parameters.AddWithValue("@partida", idPartida);
                    comando.Parameters.AddWithValue("@num", numJugada);
                    comando.Parameters.AddWithValue("@carta", cartaInicial.Id);
                    comando.ExecuteNonQuery();
                }

                transaccion.Commit();
                return idPartida;
            }
        }

        /// <summary>
        /// Devuelve la posicion en la mesa de cada jugador de la partida (id_jugador -> posicion).
        /// </summary>
        public static Dictionary<int, int> ObtenerPosiciones(int idPartida)
        {
            var posiciones = new Dictionary<int, int>();
            using (var conexion = AbrirConexion())
            using (var comando = new MySqlCommand(
                "select id_jugador, posicion from partida_jugador where id_partida = @partida", conexion))
            {
                comando.Parameters.AddWithValue("@partida", idPartida);
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                        posiciones[lector.GetInt32(0)] = lector.GetInt32(1);
                }
            }
            return posiciones;
        }

        /// <summary>
        /// Guarda un movimiento en el log (tabla jugada) con el siguiente numero de jugada.
        /// tipoAccion debe ser uno de los valores del enum de jugada.tipo_accion.
        /// </summary>
        public static void RegistrarJugada(int idPartida, int idJugador, string tipoAccion, int? idCarta)
        {
            using (var conexion = AbrirConexion())
            using (var comando = new MySqlCommand(
                "insert into jugada (id_partida, num_jugada, id_jugador, tipo_accion, id_carta) " +
                "select @partida, coalesce(max(num_jugada), 0) + 1, @jugador, @accion, @carta " +
                "from jugada where id_partida = @partida", conexion))
            {
                comando.Parameters.AddWithValue("@partida", idPartida);
                comando.Parameters.AddWithValue("@jugador", idJugador);
                comando.Parameters.AddWithValue("@accion", tipoAccion);
                comando.Parameters.AddWithValue("@carta", (object)idCarta ?? System.DBNull.Value);
                comando.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Marca la partida como terminada. Si idGanador es null, la partida queda
        /// abandonada (sin ganador). No hace nada si la partida ya estaba terminada.
        /// </summary>
        public static void TerminarPartida(int idPartida, int? idGanador)
        {
            using (var conexion = AbrirConexion())
            using (var comando = new MySqlCommand(
                "update partida set fecha_fin = now(), id_ganador = @ganador " +
                "where id_partida = @partida and fecha_fin is null", conexion))
            {
                comando.Parameters.AddWithValue("@partida", idPartida);
                comando.Parameters.AddWithValue("@ganador", (object)idGanador ?? System.DBNull.Value);
                comando.ExecuteNonQuery();
            }
        }
    }
}
