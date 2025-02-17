using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


// Namespace donde se encuentra todo el proyecto
namespace DistribucionFuenteVariable
{
    // Definicion de la clase general
    public partial class Form1 : Form
    {
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        /*                                                          CONSTANTES                                                                      */
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/

        // Tamanio del vector para los datos enviados y para el vector de los datos recibidos
        private const byte TAMANIO_DE_LOS_VECTORES_DE_COMUNICACION = 250;

        // Tamanio del header inicial
        private const byte TOTAL_DE_BYTES__HEADER_INICIAL = 3;

        // Tamanio del header final
        private const byte TOTAL_DE_BYTES__HEADER_FINAL = 3;

        // Total de bytes que estan presentes en todas las tramas de datos ( 3 Headers + 1 Terapia + 1 Placa + 1 Comando + 1 Informacion + 3 Headers )
        private const byte TOTAL_DE_DATOS_BASICOS = 10;

        // Ubicacion del byte que indica el total de datos adicionales que se envian en las tramas de datos
        private const byte UBICACION_DEL_BYTE_DE_INFORMACION = 6;


        // ---------------------------------------------------------------------------- //
        // ----------- CANTIDAD DE BYTES DE INFORMACION PARA LOS COMANDOS ------------- //
        // ---------------------------------------------------------------------------- //

        // VERIFICAR SIEMPRE QUE SEA EL ULTIMO COMANDO DEL ENUM "_comandos_recibidos" PARA QUE LAS DIMENIONES SEAN CORRECTAS
        private const byte TOTAL_COMANDOS_PARA__RECIBIR = 6;


        // ---------------------------------------------------------------------------- //
        // ----------- CANTIDAD DE BYTES DE INFORMACION PARA LOS COMANDOS ------------- //
        // ---------------------------------------------------------------------------- //

        // Coeficientes para convertir la resistencia del termistor a temperatura
        private const double COEFICIENTE_A =  1.114173758149000e-3;
        private const double COEFICIENTE_B =  2.379622600090000e-4;
        private const double COEFICIENTE_C = -2.931748694856830e-7;
        private const double COEFICIENTE_D =  9.343126802883070e-8;

        // Coeficiente para pasar de Kelvin a Celcius
        private const double KELVIN_A_CELCIUS = 273.5;

        // Resistencias en el circuito
        private const int resistenciaA_2550 = 2550;
        private const int resistenciaB_5600 = 5600;

        // Maxima cantidad de cuentas del ADC
        private const int maximasCuentasDelADC = 1023;





        // ---------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------- //
        // ------------------------------ ENUMERACIONES ------------------------------- //
        // ---------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------- //


        // ---------------------------------------------------------------------------- //
        // ------------------ BLOQUES DE ANALISIS EN LAS TRAMAS ----------------------- //
        // ---------------------------------------------------------------------------- //

        enum bloques_de_recepcion
        {
            RECEPCION_BLOQUE_DE_HEADER_INICIAL = 0,
            RECEPCION_BLOQUE_DE_TERAPIA_EN_USO,
            RECEPCION_BLOQUE_DE_PLACA_TARGET,
            RECEPCION_BLOQUE_DE_TOTAL_DE_BYTES,
            RECEPCION_BLOQUE_DE_IDENTIDICADOR_DEL_COMANDO,
            RECEPCION_BLOQUE_DE_PARAMETROS_ADICIONALES,
            RECEPCION_BLOQUE_DE_HEADER_FINAL
        };

        enum posiciones_dentro_del_vector_recibido : byte
        {
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DE_LA_TERAPIA = 0,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DE_LA_PLACA,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DEL_COMANDO,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__TOTAL_DE_BYTES,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_2,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_3,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_4,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_5,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_6,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_7,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_8,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_9,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_10,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_11,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_12,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_13,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_14,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_15,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_16,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_17,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_18,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_19,
            POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_20
        };

        enum comandos_recibidos : byte
        {
            COMANDO_RECIBIDO__DATOS_DE_LAS_MEDICIONES = 0x00,
            COMANDO_RECIBIDO__CONFIGURACION_ACTUAL_DE_LA_PLACA = 0x01
        };

        enum comandos_enviados : byte
        {
            COMANDO_ENVIADO__OK                         = 0x80,
            COMANDO_ENVIADO__DATOS_DE_LA_CONFIGURACION  = 0x81,
            COMANDO_ENVIADO__INICIAR_TERAPIA            = 0x82,
            COMANDO_ENVIADO__DETENER_TERAPIA            = 0x83,
            COMANDO_ENVIADO__PAUSAR_TERAPIA             = 0x84,
            COMANDO_ENVIADO__ACTUALIZAR_PARAMETRO       = 0x85,
            COMANDO_ENVIADO__RESPONDER_PARAMETRO        = 0x86,
            COMANDO_ENVIADO__ACTUALIZAR_LED             = 0x87
        };




        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        /*                                                          VARIABLES                                                                       */
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/

        /* VARIABLES PARA MANEJAR LA COMUNICACION SERIE */

        // Objeto para manejar el puerto serie
        private const int tasaPuertoSerie = 50000;
        private const int bitsDeDatos = 8;
        private SerialPort puertoSerie = new SerialPort("COM9", tasaPuertoSerie, Parity.None, bitsDeDatos, StopBits.One);

        // Variable para indicar el error que surja
        private string Mensaje;

        // Variables para los headers de la comunicacion
        private byte[] HeaderInicial = { 195, 62, 180 };
        private byte[] HeaderFinal = { 204, 174, 185 };

        // Se genera un buffer auxiliar para almacenar los datos del comando recibido
        private byte[] ComandoRecibido = new byte[TAMANIO_DE_LOS_VECTORES_DE_COMUNICACION];

        // Trama para almacenar los datos a enviar
        private byte[] tramaParaEnviar = new byte[TAMANIO_DE_LOS_VECTORES_DE_COMUNICACION];

        // Trama para almacenar los datos recibidos
        private byte[] tramaRecibida = new byte[TAMANIO_DE_LOS_VECTORES_DE_COMUNICACION];

        // Contador de la cantidad de bytes a enviar
        private byte totalDeBytesParaEnviar;

        // Contador de la cantidad de bytes recibidos
        private int totalDeBytesRecibidos;

        // Contador de la cantidad total de bytes por recibir en la trama
        private int totalDeBytesPorRecibir;

        // Contador de la cantidad de bytes recibidos en cada trama en particular
        private int totalDeBytesRecibidosEnEstaTrama;



        // Variable para ejecutar el Thread de recepcion de comandos y enviar los pulsos generados
        Thread threadLecturaPuertoSerie_Universal;

        // Flag para controlar la ejecucion del Thread de recepcion de comandos
        bool threadLecturaPuertoSerieIniciado_Universal = false;

        // Variable para controlar un cierre temprano del Thread
        private bool terminarThreadDeRecepcionSerie_Universal;



        // Variable para almacenar el parametro "S_TEMP1"
        private double S_TEMP1;

        // Variable para almacenar el parametro "S_TEMP2"
        private double S_TEMP2;

        // Variable para almacenar el parametro "S_TEMP3"
        private double S_TEMP3;

        // Variable para almacenar el parametro "THERM1"
        private double THERM1;

        // Variable para almacenar el parametro "THERM2"
        private double THERM2;

        // Variable para almacenar el parametro "Temperatura del termistor 1"
        private double TemperaturaDelTermistor1;

        // Variable para almacenar el parametro "Temperatura del termistor 2"
        private double TemperaturaDelTermistor2;

        // Variable para almacenar el parametro "PumpRotation"
        private double PumpRotation;

        // Variable para almacenar el parametro "SensPres"
        private double SensPres;

        // Variable para almacenar el parametro "Frecuencia elegida"
        private byte FrecuenciaElegida = 5;

        // Variable para indicar si la generacion de frecuencias de RF esta habilitada o no
        private bool habilitacionDeLaGeneracionDeRF = false;

        // Variable para almacenar el maximo valor registrado por el Termistor 1
        private double THERM1_Maximo;

        // Variable para almacenar el minimo valor registrado por el Termistor 1
        private double THERM1_Minimo;

        // Variable para almacenar el valor promedio registrado por el Termistor 1
        private double THERM1_Promedio;

        // Variable para almacenar el maximo valor registrado por el Termistor 2
        private double THERM2_Maximo;

        // Variable para almacenar el minimo valor registrado por el Termistor 2
        private double THERM2_Minimo;

        // Variable para almacenar el valor promedio registrado por el Termistor 2
        private double THERM2_Promedio;

        // Variable para obtener el acumulado total del termistor 1
        private double THERM1_Acumulado;

        // Variable para obtener el acumulado total del termistor 2
        private double THERM2_Acumulado;

        // Variable para contemplar la cantidad de mediciones que se hayan realizado del termistor 1
        private int totalDeMedicionesDelTermistor1;

        // Variable para contemplar la cantidad de mediciones que se hayan realizado del termistor 2
        private int totalDeMedicionesDelTermistor2;





        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        /*                                                  CONSTRUCTOR DEL FORMULARIO                                                              */
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        public Form1()
        {
            // Se inicializa el formulario y todos los elementos graficos
            InitializeComponent();

            // Se centra el formulario en la pantalla
            this.StartPosition = FormStartPosition.CenterScreen;

            // Para poder controlar los objetos graficos del formulario desde otros Threads sin usar delegados
            CheckForIllegalCrossThreadCalls = false;

            // Se habilita el boton para abrir el puerto serie
            btnAbrirPuerto.Enabled = true;

            // Se deshabilita el boton para cerrar el puerto serie
            btnCerrarPuerto.Enabled = false;

            // Se deshabilita el boton para iniciar el envio de datos desde la placa
            btnRecibirDatos.Enabled = false;

            // Se deshabilita el boton para detener el envio de datos desde la placa
            btnDetenerDatos.Enabled = false;

            // Se marca el flag para que el Thread se ejecute correctamente
            terminarThreadDeRecepcionSerie_Universal = false;

            // Se agregan las opciones de las terapias al ComboBox
            cmbOpcionesDefault.Items.Add("CRIO/RF");
            cmbOpcionesDefault.Items.Add("MONOP");
            cmbOpcionesDefault.Items.Add("VACIO");
            cmbOpcionesDefault.Items.Add("CR+MON");
            cmbOpcionesDefault.Items.Add("Fac/Enyg(1T)");
            cmbOpcionesDefault.Items.Add("Fac/Enyg(2T)");
            cmbOpcionesDefault.Items.Add("VACIO+MON");
            cmbOpcionesDefault.Items.Add("MON del VAC");

            // Se deja seleccionado el primer Item
            cmbOpcionesDefault.SelectedIndex = 0;

            // Se agregan las terapias disponibles
            cmbTerapias.Items.Add("Terapia de Crio SMD");
            cmbTerapias.Items.Add("Terapia de Vacio");
            cmbTerapias.Items.Add("Terapia de VCR");
            cmbTerapias.Items.Add("Terapia de HImFU");

            // Se deja seleccionado el primer Item
            cmbTerapias.SelectedIndex = 0;

        }


        // Funcion encargada de revisar que se devuelvan todos los recursos cuando se cierra la aplicación
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Se verifica si el puerto estaba abierto
            if (puertoSerie.IsOpen)
            {
                // Si el puerto estaba abierto, se lo debe cerrar
                try
                {
                    puertoSerie.Close();                // Se cierra el puerto
                }
                catch (IOException)
                {
                    Mensaje = "Ocurrio un error al cerrar el puerto serie - TerminarComunicacionSerie";
                    MessageBox.Show(Mensaje);
                }
            }

            // Se verifica si se inicio el Thread para cancelarlo
            if (threadLecturaPuertoSerieIniciado_Universal == true)
            {
                // Se cancela el thread de lectura del puerto serie
                try { threadLecturaPuertoSerie_Universal.Abort(); }
                catch (ThreadAbortException) { };
            }
        }





        // ---------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------- //
        // --------------------------- ELEMENTOS GRAFICOS ----------------------------- //
        // ---------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------- //



        // ---------------------------------------------------------------------------- //
        // ------------------------------- BOTONES ------------------------------------ //
        // ---------------------------------------------------------------------------- //


        // Handler de atencion para el evento "Click" sobre el elemento "Boton para abrir el puerto serie"
        private void btnAbrirPuerto_Click(object sender, EventArgs e)
        {
            // Se verifica el estado del puerto para saber la accion a realizar
            if (puertoSerie.IsOpen)
            {
                // Si el puerto estaba abierto, se lo debe cerrar
                try
                {
                    puertoSerie.Close();                                // Se cierra el puerto
                    lblEstadoDelPuerto.Text = "Cerrado";                // Se indica que se cerro correctamente el puerto
                    btnAbrirPuerto.Enabled = true;                      // Se habilita el boton para abrir el puerto
                    btnCerrarPuerto.Enabled = false;                    // Se deshabilita el boton para abrir el puerto
                    btnRecibirDatos.Enabled = false;                    // Se deshabilita el boton para comenzar a recibir los datos
                    btnEnviarConfiguracion.Enabled = false;             // Se deshabilita el boton para enviar la configuracion
                    btnLeerConfiguracion.Enabled = false;               // Se deshabilita el boton para recibir la configuracion

                }
                catch (IOException)
                {
                    Mensaje = "Ocurrio un error al cerrar el puerto serie - TerminarComunicacionSerie";
                    MessageBox.Show(Mensaje);
                }
            }
            else
            {
                // Se crea el formulario para seleccionar el puerto serie donde este conectada la placa
                PuertoSerie formularioConfiguracionPuertoSerie = new PuertoSerie();

                // Se lanza a ejecucion el formulario, y se capta la devolucion, para saber si se elegio correctamente un valor
                if (formularioConfiguracionPuertoSerie.ShowDialog() == DialogResult.OK)
                {
                    // Se intenta abrir el puerto
                    try
                    {
                        puertoSerie.PortName = formularioConfiguracionPuertoSerie.nombreDelPuerto;     // Se configura el nombre del puerto
                        puertoSerie.ReadTimeout = 1000;                                                // Se establece un timeout de 1 segundo para intentar abrir el puerto
                        puertoSerie.Open();                                                            // Se abre el puerto
                        Mensaje = "Puerto abierto correctamente";                                      // Se carga un mensaje de exito en la apertura
                        btnAbrirPuerto.Enabled = false;                                                // Se deshabilita el boton para abrir el puerto
                        btnCerrarPuerto.Enabled = true;                                                // Se habilita el boton para abrir el puerto
                        btnRecibirDatos.Enabled = true;                                                // Se habilita el boton para comenzar a recibir los datos
                        btnEnviarConfiguracion.Enabled = true;                                         // Se habilita el boton para enviar la configuracion
                        btnLeerConfiguracion.Enabled = true;                                           // Se habilita el boton para recibir la configuracion
                        lblEstadoDelPuerto.Text = "Abierto";                                           // Se indica que se abrio correctamente el puerto
                    }

                    catch (UnauthorizedAccessException)     // Open
                    {
                        Mensaje = "No se tienen permisos para abrir el puerto serie - EstablecerComunicacion";
                    }

                    catch (System.IO.IOException)
                    {
                        Mensaje = "Error al intentar abrir el puerto serie - EstablecerComunicacion";
                    }

                    catch (ArgumentException)               // PortName
                    {
                        Mensaje = "El nombre suministrado para el puerto no es correcto - EstablecerComunicacion";
                    }

                    catch (InvalidOperationException)       // PortName
                    {
                        if (puertoSerie.IsOpen)
                        {
                            Mensaje = "El puerto ya esta en uso - EstablecerComunicacion";
                        }
                        else
                        {
                            Mensaje = "No se puede asignar el nombre al puerto - EstablecerComunicacion";
                        }
                    }

                    MessageBox.Show(Mensaje);
                }
            }
        }


        // Handler de atencion para el evento "Click" sobre el elemento "Boton para cerrar el puerto serie"
        private void btnCerrarPuerto_Click(object sender, EventArgs e)
        {
            // Se verifica el estado del puerto para saber la accion a realizar
            if (puertoSerie.IsOpen)
            {
                // Si el puerto estaba abierto, se lo debe cerrar
                try
                {
                    puertoSerie.Close();                                // Se cierra el puerto
                    lblEstadoDelPuerto.Text = "Cerrado";           // Se indica que se cerro correctamente el puerto
                    btnAbrirPuerto.Enabled = true;                      // Se habilita el boton para abrir el puerto
                    btnCerrarPuerto.Enabled = false;                    // Se deshabilita el boton para abrir el puerto
                    btnRecibirDatos.Enabled = false;                    // Se deshabilita el boton para comenzar a recibir los datos
                    btnEnviarConfiguracion.Enabled = false;             // Se deshabilita el boton para enviar la configuracion
                    btnLeerConfiguracion.Enabled = false;               // Se deshabilita el boton para recibir la configuracion

                }
                catch (IOException)
                {
                    Mensaje = "Ocurrio un error al cerrar el puerto serie - TerminarComunicacionSerie";
                    MessageBox.Show(Mensaje);
                }
            }
        }


        // Handler de atencion para el evento "Click" sobre el elemento "Boton para iniciar el envio de datos desde la placa"
        private void btnRecibirDatos_Click(object sender, EventArgs e)
        {

            // Se debe verificar si no se esta ejecutando el Thread de recepcion
            if(threadLecturaPuertoSerieIniciado_Universal == false)
            {
                // Se deshabilita el boton para volver a lanzar el Thread de recepcion
                btnRecibirDatos.Enabled = false;

                // Se habilita el boton para detener el envio de datos
                btnDetenerDatos.Enabled = true;

                // Se habilita el boton para resetear los datos del termistor 1
                btnReiniciarValoresCaracteristicosTerm1.Enabled = true;

                // Se habilita el boton para resetear los datos del termistor 2
                btnReiniciarValoresCaracteristicosTerm2.Enabled = true;

                // Se habilita el boton para resetear los datos de ambos termistores
                btnReiniciarValoresCaracteristicosAmbosTerm.Enabled = true;

                // Se marca el flag para que el Thread se ejecute correctamente
                terminarThreadDeRecepcionSerie_Universal = false;

                // Se asigna la funcion del Thread para la recepcion de datos y se lo lanza a ejecucion
                threadLecturaPuertoSerie_Universal = new Thread(new ThreadStart(funcionThreadLecturaPuertoSerie_Universal));

                // Se lanza el Thread a ejecucion
                threadLecturaPuertoSerie_Universal.Start();

                // Se marca el flag para indicar que el Thread esta activo
                threadLecturaPuertoSerieIniciado_Universal = true;

                // Se envia el comando para que la placa inicie el envio continuo de datos de los sensores
//                EnviarComando((byte)comandos_enviados.COMANDO_ENVIADO__INICIAR_ENVIO_DE_DATOS);
            }

        }


        // Handler de atencion para el evento "Click" sobre el elemento "Boton para detener el envio de datos desde la placa"
        private void btnDetenerDatos_Click(object sender, EventArgs e)
        {

            // Se debe verificar si se esta ejecutando el Thread de recepcion
            if (threadLecturaPuertoSerieIniciado_Universal == true)
            {
                // Se habilita el boton para volver a lanzar el Thread de recepcion
                btnRecibirDatos.Enabled = true;

                // Se deshabilita el boton para detener el envio de datos
                btnDetenerDatos.Enabled = false;

                // Se deshabilita el boton para resetear los datos del termistor 1
                btnReiniciarValoresCaracteristicosTerm1.Enabled = false;

                // Se deshabilita el boton para resetear los datos del termistor 2
                btnReiniciarValoresCaracteristicosTerm2.Enabled = false;

                // Se deshabilita el boton para resetear los datos de ambos termistores
                btnReiniciarValoresCaracteristicosAmbosTerm.Enabled = false;

                // Se marca el flag para que el Thread se termine
                terminarThreadDeRecepcionSerie_Universal = true;

                // Se envia el comando para que la placa detenga el envio continuo de datos de los sensores
//                EnviarComando((byte)comandos_enviados.COMANDO_ENVIADO__DETENER_ENVIO_DE_DATOS);
            }

        }


        // Handler de atencion para el evento "Click" sobre el elemento "Boton para enviar la configuracion hacia la placa"
        private void btnEnviarConfiguracion_Click(object sender, EventArgs e)
        {
            // Se envia el comando para actualizar la configuracion
//            EnviarComando( (byte) comandos_enviados.COMANDO_ENVIADO__ACTUALIZAR_CONFIGURACION );
        }



        // -------------------------------------------------------------------------------- //
        // ------------------------------- RADIOBUTTON ------------------------------------ //
        // -------------------------------------------------------------------------------- //


        // Handler de atencion para el evento "Click" sobre el elemento "Seleccion de la frecuencia de 500k para RF"
        private void rbnFrecuenciaDeRf500_CheckedChanged(object sender, EventArgs e)
        {
            // Se actualiza el valor de la frecuencia elegida
            FrecuenciaElegida = 5;
        }


        // Handler de atencion para el evento "Click" sobre el elemento "Seleccion de la frecuencia de 800k para RF"
        private void rbnFrecuenciaDeRf800_CheckedChanged(object sender, EventArgs e)
        {
            // Se actualiza el valor de la frecuencia elegida
            FrecuenciaElegida = 8;
        }


        // Handler de atencion para el evento "Click" sobre el elemento "Seleccion de la frecuencia de 1000k para RF"
        private void rbnFrecuenciaDeRf1000_CheckedChanged(object sender, EventArgs e)
        {
            // Se actualiza el valor de la frecuencia elegida
            FrecuenciaElegida = 10;
        }




        // -------------------------------------------------------------------------------- //
        // --------------------------------- CHECKBOX ------------------------------------- //
        // -------------------------------------------------------------------------------- //


        // Handler de atencion para el evento "Click" sobre el elemento "Modificar la habilitacion de RF"
        private void cbxHabilitarFrec_CheckedChanged(object sender, EventArgs e)
        {
            // Se verifica el estado del control, para cambiar la habilitacion de la generacion de RF
            if( cbxHabilitarFrec.Checked )
            {
                // Se habilita la generacion de RF
                habilitacionDeLaGeneracionDeRF = true;

                // Se habilitan los controles graficos para modificar los parametros de RF
                cbxDutyTicks.Enabled = true;
                rbnFrecuenciaDeRf1000.Enabled = true;
                rbnFrecuenciaDeRf500.Enabled = true;
                rbnFrecuenciaDeRf800.Enabled = true;

                // Se verifica si el control se realiza por Duty o por Ticks
                if(cbxDutyTicks.Checked)
                {
                    numDuty.Enabled = true;
                    numTicks.Enabled = false;
                }
                else
                {
                    numDuty.Enabled = false;
                    numTicks.Enabled = true;
                }

            }

            else
            {
                // Se deshabilita la generacion de RF
                habilitacionDeLaGeneracionDeRF = false;

                // Se deshabilitan los controles graficos para modificar los parametros de RF
                cbxDutyTicks.Enabled = false;
                numDuty.Enabled = false;
                numTicks.Enabled = false;
                rbnFrecuenciaDeRf1000.Enabled = false;
                rbnFrecuenciaDeRf500.Enabled = false;
                rbnFrecuenciaDeRf800.Enabled = false;
            }
        }


        // Handler de atencion para el evento "Click" sobre el elemento "Modificar la habilitacion del PWM"
        private void cbxHabilitarPWM_CheckedChanged(object sender, EventArgs e)
        {
            // Se verifica el estado del control, para cambiar la habilitacion del control del PWM
            if (cbxHabilitarPWM.Checked)
            {
                // Se habilita el control grafico para modificar el PWM de la fuente
                numPWM.Enabled = true;
            }
            else
            {
                // Se deshabilita el control grafico para modificar el PWM de la fuente
                numPWM.Enabled = false;
            }
        }


        // Handler de atencion para el evento "Click" sobre el elemento "Seleccionar Duty o Ticks"
        private void cbxDutyTicks_CheckedChanged(object sender, EventArgs e)
        {
            // Se verifica si el control se realiza por Duty o por Ticks
            if (cbxDutyTicks.Checked)
            {
                numDuty.Enabled = true;
                numTicks.Enabled = false;
            }
            else
            {
                numDuty.Enabled = false;
                numTicks.Enabled = true;
            }
        }












        // ---------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------- //
        // --------------------- FUNCIONES PARA LO COMUNICACION ----------------------- //
        // ---------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------- //



        // ---------------------------------------------------------------------------- //
        // ------------------ THREAD PARA LA RECEPCION DE DATOS ----------------------- //
        // ---------------------------------------------------------------------------- //

        // Este es el thread que se encarga de la recepción de datos por el puerto serie
        private void funcionThreadLecturaPuertoSerie_Universal()
        {
            // ------------------------------- DEFINICION DE VARIABLES ------------------------------------ //

            // Se definen variables auxiliares, para hacer mas legible el codigo
            int TimeOutRequerido;
            int TotalDeBytesPorRecibir;
            int IntentosDeLectura;

            // Copia auxiliar del byte recibido
            byte byteRecibido;


            // Bufer para los comandos que se pueden recibir
            byte[] ComandosAceptadosParaRecibir = new byte[TOTAL_COMANDOS_PARA__RECIBIR];


            // --------------------- AGREGADO DE LOS COMANDOS VALIDOS PARA RECIBIR -------------------------- //

            // Se rellena el vector con los comandos que se pueden recibir
            ComandosAceptadosParaRecibir[0] = (byte)comandos_recibidos.COMANDO_RECIBIDO__DATOS_DE_LAS_MEDICIONES;


            // ------------------------------- INICIALIZACION DE LAS VARIABLES ------------------------------------ //

            // Indicador del bloque en recepcion
            bloques_de_recepcion bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL;

            // Contador del total de bytes recibidos correctamente del header inicial
            byte bytesDelHeaderInicialRecibidos = 0;

            // Contador del total de bytes recibidos correctamente del header final
            byte bytesDelHeaderFinalRecibidos = 0;

            // Indice auxiliar para recorrer el vector de comandos aceptados para recibir
            int indiceDeComandosAceptadosParaRecibir;

            // Contador de parametros adicionales por recibir en cada comando
            byte parametrosAdicionalesPorRecibir;

            // Contador de parametros adicionales recibidos en cada comando
            byte parametrosAdicionalesRecibidos;

            // Flag para indicar que hay un nuevo comando para ejecutar
            bool nuevoComandoParaEjecutar;

            // Se inicializa la variable, solo para que el compilador no marque error
            parametrosAdicionalesPorRecibir = 0;

            // Se inicializa la variable, solo para que el compilador no marque error
            parametrosAdicionalesRecibidos = 0;

            // Se inicializa la variable, solo para que el compilador no marque error
            nuevoComandoParaEjecutar = false;

            // Se define un timeout de 1 segundo
            TimeOutRequerido = 1000;

            // Se define la cantidad de bytes a recibir
            TotalDeBytesPorRecibir = 40;

            // Se definen la cantidad de intentos de lectura
            IntentosDeLectura = 2;

            // Se borra el contador de bytes leidos, por las dudas
            totalDeBytesRecibidos = 0;


            // ------------------------------- LOOP PERPETUO ------------------------------------ //

            while (terminarThreadDeRecepcionSerie_Universal == false)
            {

                // Se borra el buffer de recepcion para no tener datos anteriores
                Array.Clear(tramaRecibida, 0, tramaRecibida.Length);

                // Se lee la trama de datos enviada por el micro
                LeerTramaRecibida(TimeOutRequerido, TotalDeBytesPorRecibir, IntentosDeLectura);

                // Si no se leyo ningun dato, se vuelve a esperar
                if (totalDeBytesRecibidos == 0)
                {
                    // Se vuelve a ejecutar el loop desde el inicio
                    continue;
                }


                // Se borran los posibles datos que haya en el buffer despues de realizar la lectura
                puertoSerie.DiscardInBuffer();


                // Se tiene que hacer un loop para recorrer todos los bytes recibidos
                for (int indiceVectorRecibido = 0; indiceVectorRecibido < totalDeBytesRecibidos; indiceVectorRecibido++)
                {
                    // Se genera una copia auxiliar del byte
                    byteRecibido = tramaRecibida[indiceVectorRecibido];

                    // Se procesa el byte recibido en funcion de la parte de la trama que ubique
                    switch (bloqueEnRecepcion)
                    {
                        // --------------------- 1) Los primeros bytes de la trama son el encabezado inicial ----------------------- //
                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL:

                            // Se verifica la correcta ubicacion de cada byte recibido del header inicial
                            if (HeaderInicial[bytesDelHeaderInicialRecibidos] == byteRecibido)
                            {
                                // Se incrementa el contador de bytes del header inicial recibidos
                                bytesDelHeaderInicialRecibidos++;

                                // Se verifica si se recibieron correctamente todos los bytes del header inicial recibidos
                                if (bytesDelHeaderInicialRecibidos >= TOTAL_DE_BYTES__HEADER_INICIAL)
                                {
                                    // Se pasa al bloque de recepcion de la terapia en uso
                                    bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_TERAPIA_EN_USO;

                                    // Se resetea el contador de bytes del header inicial recibidos
                                    bytesDelHeaderInicialRecibidos = 0;
                                }
                            }
                            // Caso contrario, si algun byte no coincide, se reinicia el proceso de recepcion de la trama
                            else
                            {
                                // Se resetea el selector del bloque en recepcion
                                bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL;

                                // Se resetea el contador de bytes del header inicial recibidos
                                bytesDelHeaderInicialRecibidos = 0;

                                // Se resetea el contador de bytes del header final recibidos
                                bytesDelHeaderFinalRecibidos = 0;

                                // Se resetea el contador de parametros adicionales recibidos
                                parametrosAdicionalesRecibidos = 0;
                            }

                            break;  // Fin de "case RECEPCION_BLOQUE_DE_HEADER_INICIAL"


                        // ------------------- 2) Luego se encuentra el dato de la terapia en uso ------------------- //

                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_TERAPIA_EN_USO:

                            // Se almacena el comando recibido
                            ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DEL_COMANDO] = byteRecibido;

                            // Se pasa al bloque de recepcion de la placa con la que se quiere comunicar
                            bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_PLACA_TARGET;

                            break;


                        // ------------------- 3) Luego se encuentra el dato de la placa con la que se quiere comunicar ------------------- //

                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_PLACA_TARGET:

                            // Se almacena el comando recibido
                            ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DEL_COMANDO] = byteRecibido;

                            // Se pasa al bloque de recepcion del comando a ejecutar
                            bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_IDENTIDICADOR_DEL_COMANDO;

                            break;


                        // ------------------- 4) Luego se encuentra el dato del comando a ejecutar ------------------- //
                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_IDENTIDICADOR_DEL_COMANDO:

                            // Se recorre el vector de todos los comandos aceptados para recibir
                            for (indiceDeComandosAceptadosParaRecibir = 0; indiceDeComandosAceptadosParaRecibir < TOTAL_COMANDOS_PARA__RECIBIR; indiceDeComandosAceptadosParaRecibir++)
                            {
                                // Se revisa que sea un comando valido
                                if (ComandosAceptadosParaRecibir[indiceDeComandosAceptadosParaRecibir] == byteRecibido)
                                {
                                    // Se almacena el comando recibido
                                    ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DEL_COMANDO] = byteRecibido;

                                    // Se pasa al bloque de recepcion de la cantidad de bytes de informacion
                                    bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_TOTAL_DE_BYTES;

                                    // Se corta el loop al verificar que es un comando valido
                                    break;
                                }
                            }

                            // Si el loop finalizo por desborde, no se encontro el dato dentro de los comandos validos
                            if (indiceDeComandosAceptadosParaRecibir == TOTAL_COMANDOS_PARA__RECIBIR)
                            {
                                // Se resetea el selector del bloque en recepcion
                                bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL;

                                // Se resetea el contador de bytes del header inicial recibidos
                                bytesDelHeaderInicialRecibidos = 0;

                                // Se resetea el contador de bytes del header final recibidos
                                bytesDelHeaderFinalRecibidos = 0;

                                // Se resetea el contador de parametros adicionales recibidos
                                parametrosAdicionalesRecibidos = 0;
                            }

                            break;  // Fin de "case RECEPCION_BLOQUE_DE_IDENTIDICADOR_DEL_COMANDO"



                        // ------------------- 5) Luego se encuentra el dato de la cantidad de bytes a recibir ------------------- //
                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_TOTAL_DE_BYTES:

                            // Se almacena el dato recibido dentro del vector de recepcion de comandos
                            ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__TOTAL_DE_BYTES] = byteRecibido;

                            // Se ramifica la recepcion, segun si el comando necesita recibir datos adicionales o no
                            if (byteRecibido != 0)
                            {
                                // Se pasa al estado de recepcion del siguiente bloque
                                bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_PARAMETROS_ADICIONALES;

                                // Se actualiza el contador de parametros adicionales que este comando espera recibir
                                parametrosAdicionalesPorRecibir = byteRecibido;

                                // Se resetea el contador de parametros adicionales recibidos
                                parametrosAdicionalesRecibidos = 0;
                            }

                            // Si el comando no necesita recibir datos adicionales, se pasa a la recepcion del header final
                            else
                            {
                                // Se pasa al bloque de recepcion del bloque del header final, ya que no tiene datos adicionales
                                bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_FINAL;

                                // Se resetea el contador de bytes del header final recibidos
                                bytesDelHeaderFinalRecibidos = 0;
                            }

                            break;  // Fin de "case RECEPCION_BLOQUE_DE_TOTAL_DE_BYTES"



                        // -------------------- 6) Luego se encuentran los datos necesarios para los comandos complejos -------------------- //
                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_PARAMETROS_ADICIONALES:

                            // Se almacena el dato recibido dentro del vector de recepcion de comandos
                            ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesRecibidos] = byteRecibido;

                            // Se incrementa el contador de parametros adicionales recibidos
                            parametrosAdicionalesRecibidos++;

                            // Se verifica si se recibieron correctamente todos los parametros adicionales que necesita este comando
                            if (parametrosAdicionalesRecibidos >= parametrosAdicionalesPorRecibir)
                            {

                                // Se pasa al bloque de recepcion del header final
                                bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_FINAL;

                                // Se resetea el contador de bytes del header final recibidos
                                bytesDelHeaderFinalRecibidos = 0;

                            }

                            break;  // Fin de "case RECEPCION_BLOQUE_DE_PARAMETROS_ADICIONALES"



                        // ----- 7) Finalmente, para cerrar correctamente la trama, se debe colocar el header final ------ //
                        case bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_FINAL:

                            // Se verifica la correcta ubicacion de cada byte recibido del header final
                            if (HeaderFinal[bytesDelHeaderFinalRecibidos] == byteRecibido)
                            {
                                // Se incrementa el contador de bytes del header final recibidos
                                bytesDelHeaderFinalRecibidos++;

                                // Se verifica si se recibieron correctamente todos los bytes del header final recibidos
                                if (bytesDelHeaderFinalRecibidos >= TOTAL_DE_BYTES__HEADER_FINAL)
                                {
                                    // Se pasa al bloque de recepcion del header inicial para la siguiente trama
                                    bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL;

                                    // Se recibio una trama correctamente. Se marca el flag para indicar que se dehe ejecutar la accion
                                    nuevoComandoParaEjecutar = true;

                                    // Se resetea el selector del bloque en recepcion
                                    bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL;

                                    // Se resetea el contador de bytes del header inicial recibidos
                                    bytesDelHeaderInicialRecibidos = 0;

                                    // Se resetea el contador de bytes del header final recibidos
                                    bytesDelHeaderFinalRecibidos = 0;
                                }
                            }
                            // Caso contrario, si algun byte no coincide, se reinicia el proceso de recepcion de la trama
                            else
                            {
                                // Se resetea el selector del bloque en recepcion
                                bloqueEnRecepcion = bloques_de_recepcion.RECEPCION_BLOQUE_DE_HEADER_INICIAL;

                                // Se resetea el contador de bytes del header inicial recibidos
                                bytesDelHeaderInicialRecibidos = 0;

                                // Se resetea el contador de bytes del header final recibidos
                                bytesDelHeaderFinalRecibidos = 0;

                                // Se resetea el contador de parametros adicionales recibidos
                                parametrosAdicionalesRecibidos = 0;
                            }

                            break;  // fin de "case RECEPCION_BLOQUE_DE_HEADER_FINAL"
                    }
                }



                // Se borra el contador de bytes recibidos en la ultima trama, por las dudas que no se borre en la funcion de recepcion
                totalDeBytesRecibidos = 0;

                // Se revisa si hay un nuevo comando para ejecutar
                if (nuevoComandoParaEjecutar == true)
                {
                    // Se borra el flag, para indicar que ya se ejecuto el comando recibido
                    nuevoComandoParaEjecutar = false;

                    // Se ejecuta la funcion para llevar a cabo las acciones de cada comando recibido
                    EjecutarComandoRecibido();

                    // Se borra el buffer del comando recibido, para mayor claridad en el debugueo
                    Array.Clear(ComandoRecibido, 0, ComandoRecibido.Length);

                }

            }

            // Se indica que el Thread no esta mas en ejecucion
            threadLecturaPuertoSerieIniciado_Universal = false;

            // Se cancela el thread de lectura del puerto serie
            try { threadLecturaPuertoSerie_Universal.Abort(); }
            catch (ThreadAbortException) { };

        }



        // ---------------------------------------------------------------------------- //
        // -------------------- FUNCION PARA EL ENVIO DE DATOS ------------------------ //
        // ---------------------------------------------------------------------------- //

        // Funcion para enviar los comandos
        private void EnviarComando(byte comandoParaEnviar)
        {
            // Se reinicia el contador de bytes a enviar
            totalDeBytesParaEnviar = 0;

            // Se agrega el Header inicial
            agregarHeaderInicial();

            // Se agrega la terapia en uso
            agregarTerapiaEnUso();

            // Se agrega la placa destinada


            // Se agrega el comando a la trama
            agregarComando(comandoParaEnviar);

            // Se agrega el Header final
            agregarHeaderFinal();

            // Se actualiza el total de bytes de informacion que se envian en el comando
            agregarTotalDeBytesDeInformacion();

            // Se envia la trama
            enviarTrama();
        }



        // Funcion para agregar el Header inicial a la trama para enviar
        private void agregarHeaderInicial()
        {
            // Se coloca el header inicial
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderInicial[0];
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderInicial[1];
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderInicial[2];
        }



        // Funcion para agregar el dato de la terapia en uso
        private void agregarTerapiaEnUso()
        {
            // Se coloca el header inicial
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderInicial[0];
        }



        // Funcion para agregar el Header final a la trama para enviar
        private void agregarHeaderFinal()
        {
            // Se coloca el header inicial
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderFinal[0];
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderFinal[1];
            tramaParaEnviar[totalDeBytesParaEnviar++] = HeaderFinal[2];
        }



        // Funcion para agregar el total de bytes de informacion a la trama para enviar
        private void agregarTotalDeBytesDeInformacion()
        {
            // Se indica la cantidad de bytes de informacion que lleva el comando
            tramaParaEnviar[UBICACION_DEL_BYTE_DE_INFORMACION] = (byte)(totalDeBytesParaEnviar - TOTAL_DE_DATOS_BASICOS);
        }



        // Funcion para enviar la trama
        private void enviarTrama()
        {
            // Se envia la trama por el puerto serie
            puertoSerie.Write(tramaParaEnviar, 0, totalDeBytesParaEnviar);
        }



        // ---------------------------------------------------------------------------- //
        // ------------------- FUNCION PARA RECIBIR UNA TRAMA ------------------------- //
        // ---------------------------------------------------------------------------- //

        // Funcion para leer una trama por el puerto serie, segun los parametros suministrados
        //////// CODIGOS DE ERROR DEVUELTOS
        //////// 0 => Trama recibida correcta
        //////// 1 => Error de timeout para la lectura de una trama
        //////// 2 => Error por agotar todas las instancias para leer la trama sin completarla

        private int LeerTramaRecibida(int TimeOutRequerido, int TotalDeBytesPorRecibir, int IntentosDeLectura)
        {
            // Se define el timeout requerido para recibir la respuesta
            puertoSerie.ReadTimeout = TimeOutRequerido;

            // Se actualiza la cantidad total de bytes que se esperan recibir en esta trama
            totalDeBytesPorRecibir = TotalDeBytesPorRecibir;

            // Se resetea el contador de bytes recibidos en esta trama
            totalDeBytesRecibidos = 0;

            // Se realizan los intentos de lectura requeridos para completar la trama
            for (int indiceDeLectura = 0; indiceDeLectura < IntentosDeLectura;)
            {
                // Se intenta la lectura de una trama
                try
                {
                    // Se lee una trama, teniendo en cuenta los bytes ya recibidos en los sucesivos intentos
                    totalDeBytesRecibidosEnEstaTrama = puertoSerie.Read(tramaRecibida, totalDeBytesRecibidos, totalDeBytesPorRecibir - totalDeBytesRecibidos);

                    // Se verifica que se haya leido algo
                    if (totalDeBytesRecibidosEnEstaTrama != 0)
                    {
                        // Se actualiza el contador de bytes recibidos
                        totalDeBytesRecibidos += totalDeBytesRecibidosEnEstaTrama;

                        // Se verifica si ya se leyeron todos los datos que se esperaban
                        if (totalDeBytesRecibidos == totalDeBytesPorRecibir)
                        {
                            // Se completo correctamente la lectura de la trama
                            return (0);
                        }

                        // Se incrementa el contador de intentos de lectura
                        indiceDeLectura++;
                    }
                }
                catch (TimeoutException)
                {
                    // Se produjo una excepcion por agotar el tiempo limite de espera en la recepcion de los datos
                    return (1);
                }
            }

            // Se agotaron los intentos de lectura sin poder completar la trama
            return (2);

        }




















        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------- FUNCIONES PARA EL USUARIO FINAL --------------------------------- //
        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------------------------- //
        // ---------------------------------------------------------------------------------------------- //



        // ---------- FUNCION PARA LISTAR LAS ORDENES A EJECUTAR PARA CADA COMANDO RECIBIDO
        private void EjecutarComandoRecibido ()
        {
            // Contador de los parametros adicionales ya analizados
            byte parametrosAdicionalesAnalizados;

            // Se resetea el contador de parametros adicionales ya analizados
            parametrosAdicionalesAnalizados = 0;

            // Se acciona segun el comando recibido
            switch (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__ID_DEL_COMANDO])
            {

                // Comando para devolver la cantidad de pulsos consumidos desde la ultima consulta
                case (byte)comandos_recibidos.COMANDO_RECIBIDO__DATOS_DE_LAS_MEDICIONES:

                    // El primer dato es el valor de cuentas para "THERM1"
                    THERM1 = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    THERM1 += (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++] << 8);
                    lblValueTherm1.Text = THERM1.ToString("N0");

                    // El segundo dato es el valor de cuentas para "THERM2"
                    THERM2 = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    THERM2 += (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++] << 8);
                    lblValueTherm2.Text = THERM2.ToString("N0");

                    // El tercer dato es el valor del caudalimetro
                    PumpRotation = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    lblValuePumpRotation.Text = PumpRotation.ToString("N0");

                    // El cuarto dato es el valor de cuentas para "THERM2"
                    SensPres = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    SensPres += (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++] << 8);
                    lblValueSensPres.Text = SensPres.ToString("N0");

                    // El cuarto dato es el valor de cuentas para "S_TEMP1"
                    S_TEMP1 = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    S_TEMP1 += (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++] << 8);
                    S_TEMP1 /= 10;
                    lblValueSTemp1.Text = S_TEMP1.ToString("N0");

                    // El cuarto dato es el valor de cuentas para "S_TEMP2"
                    S_TEMP2 = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    S_TEMP2 += (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++] << 8);
                    S_TEMP2 /= 10;
                    lblValueSTemp2.Text = S_TEMP2.ToString("N0");

                    // El cuarto dato es el valor de cuentas para "S_TEMP3"
                    S_TEMP3 = ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++];
                    S_TEMP3 += (ComandoRecibido[(byte)posiciones_dentro_del_vector_recibido.POSICION_DENTRO_DEL_VECTOR_RECIBIDO__PARAMETRO_1 + parametrosAdicionalesAnalizados++] << 8);
                    S_TEMP3 /= 10;
                    lblValueSTemp3.Text = S_TEMP3.ToString("N0");


                    // Actualizar el valor de la temperatura calculada por el sensor "THERM1"
                    CalcularTemperaturaDelTermistor(1);

                    // Actualizar el valor de la temperatura calculada por el sensor "THERM2"
                    CalcularTemperaturaDelTermistor(2);

                    // Se actualiza la indicacion en pantalla para el parametro "Temperatura del termistor 1"
                    lblValueTemp1.Text = TemperaturaDelTermistor1.ToString("N2");

                    // Se actualiza la indicacion en pantalla para el parametro "Temperatura del termistor 2"
                    lblValueTemp2.Text = TemperaturaDelTermistor2.ToString("N2");


                    // Se incrementa el contador de mediciones del termistor 1
                    totalDeMedicionesDelTermistor1++;

                    // Se verifica si es la primer medicion de los parametros
                    if (totalDeMedicionesDelTermistor1 == 1)
                    {
                        // Se toman los valores caracteristicos del termistor 1
                        THERM1_Maximo = THERM1;
                        THERM1_Minimo = THERM1;
                        THERM1_Promedio = THERM1;
                        THERM1_Acumulado = THERM1;
                    }
                    else
                    {
                        // Se actualizan los valores caracteristicos del termistor 1
                        if (THERM1_Maximo < THERM1)
                        {
                            THERM1_Maximo = THERM1;
                        }
                        if (THERM1_Minimo > THERM1)
                        {
                            THERM1_Minimo = THERM1;
                        }

                        // Se suma el valor al acumulado del termistor 1
                        THERM1_Acumulado += THERM1;

                        // Se verifica que el total de mediciones no sea 0, para evitar errores
                        if (totalDeMedicionesDelTermistor1 != 0)
                        {
                            // Se calcula el promedio historico del termistor 1
                            THERM1_Promedio = THERM1_Acumulado / totalDeMedicionesDelTermistor1;
                        }
                    }





                    // Se incrementa el contador de mediciones del termistor 2
                    totalDeMedicionesDelTermistor2++;

                    // Se verifica si es la primer medicion de los parametros
                    if (totalDeMedicionesDelTermistor2 == 1)
                    {
                        // Se toman los valores caracteristicos del termistor 2
                        THERM2_Maximo = THERM2;
                        THERM2_Minimo = THERM2;
                        THERM2_Promedio = THERM2;
                        THERM2_Acumulado = THERM2;
                    }
                    else
                    {
                        // Se actualizan los valores caracteristicos del termistor 2
                        if (THERM2_Maximo < THERM2)
                        {
                            THERM2_Maximo = THERM2;
                        }
                        if (THERM2_Minimo > THERM2)
                        {
                            THERM2_Minimo = THERM2;
                        }

                        // Se suma el valor al acumulado del termistor 2
                        THERM2_Acumulado += THERM2;

                        // Se verifica que el total de mediciones no sea 0, para evitar errores
                        if (totalDeMedicionesDelTermistor2 != 0)
                        {
                            // Se calcula el promedio historico del termistor 2
                            THERM2_Promedio = THERM2_Acumulado / totalDeMedicionesDelTermistor2;
                        }

                    }
                    



                    // Se actualiza la indicacion en pantalla para el parametro "Maximo del termistor 1"
                    lblMaxTherm1.Text = THERM1_Maximo.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Minimo del termistor 1"
                    lblMinTherm1.Text = THERM1_Minimo.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Promedio del termistor 1"
                    lblPromTherm1.Text = THERM1_Promedio.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Maximo del termistor 2"
                    lblMaxTherm2.Text = THERM2_Maximo.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Minimo del termistor 2"
                    lblMinTherm2.Text = THERM2_Minimo.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Promedio del termistor 2"
                    lblPromTherm2.Text = THERM2_Promedio.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Mediciones del termistor 1"
                    lblMedicionesTherm1.Text = totalDeMedicionesDelTermistor1.ToString("N0");

                    // Se actualiza la indicacion en pantalla para el parametro "Mediciones del termistor 2"
                    lblMedicionesTherm2.Text = totalDeMedicionesDelTermistor2.ToString("N0");


                    break;
            }
        }





        // ---------- FUNCION PARA PREPARAR EL BUFFER DE DATOS SEGUN EL COMANDO QUE SE QUIERA ENVIAR
        private void agregarComando(byte comandoParaEnviar)
        {
            // Se eligen las opciones segun el comando que se deba enviar
            switch (comandoParaEnviar)
            {
                // Enviar el estado de la configuracion
                case (byte)comandos_enviados.COMANDO_ENVIADO__PAUSAR_TERAPIA:

                    // ---------------- BYTE CON EL CODIGO DEL COMANDO

                    // Se coloca el identificador del comando
                    agregarComandoParaEnviar(comandoParaEnviar);


                    // ---------------- BYTE CON EL TOTAL DE DATOS DE INFORMACION (Se completa al final)

                    // Se lo deja en 0, y se completa al terminar de agregar los bytes de informacion
                    reservarByteParaElTotalDeInformacion();


                    // ---------------- BYTE DE INFORMACION

                        // ---------------- ENABLES

                    // Se envia el dato de la activacion del parametro "Enable 1"
                    agregarInformacionBooleanaParaEnviar( cbxEnable1.Checked );

                    // Se envia el dato de la activacion del parametro "Enable 2"
                    agregarInformacionBooleanaParaEnviar( cbxEnable2.Checked );

                    // Se envia el dato de la activacion del parametro "Enable 3"
                    agregarInformacionBooleanaParaEnviar( cbxEnable3.Checked );

                    // Se envia el dato de la activacion del parametro "Enable 4"
                    agregarInformacionBooleanaParaEnviar( cbxEnable4.Checked );

                    // Se envia el dato de la activacion del parametro "Enable 5"
                    agregarInformacionBooleanaParaEnviar( cbxEnable5.Checked );

                        // ---------------- RELAYS

                    // Se envia el dato de la activacion del parametro "Relay 1"
                    agregarInformacionBooleanaParaEnviar(cbxRelay1.Checked);

                    // Se envia el dato de la activacion del parametro "Relay 5"
                    agregarInformacionBooleanaParaEnviar(cbxRelay2.Checked);

                    // Se envia el dato de la activacion del parametro "Relay 4"
                    agregarInformacionBooleanaParaEnviar(cbxRelay3.Checked);

                    // Se envia el dato de la activacion del parametro "Relay 3"
                    agregarInformacionBooleanaParaEnviar(cbxRelay4.Checked);

                    // Se envia el dato de la activacion del parametro "Relay 2"
                    agregarInformacionBooleanaParaEnviar(cbxRelay5.Checked);

                        // ---------------- RESTO DE LAS OPCIONES

                    // Se envia el dato de la activacion del parametro "Peltier Vacio"
                    agregarInformacionBooleanaParaEnviar(cbxPeltierV.Checked);

                    // Se envia el dato de la activacion del parametro "Fan"
                    agregarInformacionBooleanaParaEnviar(cbxFan.Checked);

                    // Se envia el dato de la activacion del parametro "Fans"
                    agregarInformacionBooleanaParaEnviar(cbxFans.Checked);

                    // Se envia el dato de la activacion del parametro "Pump"
                    agregarInformacionBooleanaParaEnviar(cbxPump.Checked);

                    // Se envia el dato de la activacion del parametro "Peltier de crio"
                    agregarInformacionBooleanaParaEnviar(cbxPeltier.Checked);

                    // Se envia el dato de la activacion del parametro "Leds de crio"
                    agregarInformacionBooleanaParaEnviar(cbxLedsCrio.Checked);

                    // Se envia el dato de la activacion del parametro "Led 1"
                    agregarInformacionBooleanaParaEnviar(cbxLed1.Checked);

                    // Se envia el dato de la activacion del parametro "Led 2"
                    agregarInformacionBooleanaParaEnviar(cbxLed2.Checked);

                    // Se envia el dato de la activacion del parametro "Led 3"
                    agregarInformacionBooleanaParaEnviar(cbxLed3.Checked);

                        // ---------------- FUENTE DE PWM

                    // Se envia el dato de la activacion del parametro "Habilitar PWM"
                    agregarInformacionBooleanaParaEnviar(cbxHabilitarPWM.Checked);

                    // Se agrega la informacion del controlador para seleccionar el porcentaje de PWM
                    agregarInformacionIntParaEnviar( numPWM.Value );

                        // ---------------- FUENTE DE FRECUENCIA

                    // Se envia el dato de la activacion del parametro "Habilitar Frecuencia"
                    agregarInformacionBooleanaParaEnviar(cbxHabilitarFrec.Checked);

                    // Se envia el dato de la frecuencia elegida para RF
                    agregarInformacionIntParaEnviar(FrecuenciaElegida);

                    // Se envia el dato de la activacion del parametro "Duty / Ticks"
                    agregarInformacionBooleanaParaEnviar(cbxDutyTicks.Checked);

                    // Se verifica la opcion seleccionada, para confirmar el dato a enviar
                    if(cbxDutyTicks.Checked)
                    {
                        // Si esta seleccionado, se envia el Duty
                        agregarInformacionIntParaEnviar(numDuty.Value);
                    }
                    else
                    {
                        // Si no esta seleccionado, se envian los ticks
                        agregarInformacionIntParaEnviar(numTicks.Value);
                    }


                    // Fin de la secuencia para este comando
                    break;


                // Enviar el estado de la configuracion
                case (byte)comandos_enviados.COMANDO_ENVIADO__DETENER_TERAPIA:

                    // ---------------- BYTE CON EL CODIGO DEL COMANDO

                    // Se coloca el identificador del comando
                    agregarComandoParaEnviar(comandoParaEnviar);


                    // ---------------- BYTE CON EL TOTAL DE DATOS DE INFORMACION (Se completa al final)

                    // Se lo deja en 0, y se completa al terminar de agregar los bytes de informacion
                    reservarByteParaElTotalDeInformacion();

                    break;


                // Enviar el estado de la configuracion
                case (byte)comandos_enviados.COMANDO_ENVIADO__INICIAR_TERAPIA:

                    // ---------------- BYTE CON EL CODIGO DEL COMANDO

                    // Se coloca el identificador del comando
                    agregarComandoParaEnviar(comandoParaEnviar);


                    // ---------------- BYTE CON EL TOTAL DE DATOS DE INFORMACION (Se completa al final)

                    // Se lo deja en 0, y se completa al terminar de agregar los bytes de informacion
                    reservarByteParaElTotalDeInformacion();

                    break;




                // Enviar el estado de la configuracion
                case (byte)comandos_enviados.COMANDO_ENVIADO__ACTUALIZAR_LED:

                    // ---------------- BYTE CON EL CODIGO DEL COMANDO

                    // Se coloca el identificador del comando
                    agregarComandoParaEnviar(comandoParaEnviar);


                    // ---------------- BYTE CON EL TOTAL DE DATOS DE INFORMACION (Se completa al final)

                    // Se lo deja en 0, y se completa al terminar de agregar los bytes de informacion
                    reservarByteParaElTotalDeInformacion();


                    // ---------------- BYTE DE INFORMACION

                    // Se envia el dato de la activacion del parametro "Led 1"
                    agregarInformacionBooleanaParaEnviar(cbxLed1.Checked);

                    break;

            }
        }






        // Funcion para agregar el comando requerido
        void agregarComandoParaEnviar ( byte comando )
        {
            // Se coloca el identificador del comando
            tramaParaEnviar[totalDeBytesParaEnviar] = comando;

            // Se incrementa el contador de parametros utilizados
            totalDeBytesParaEnviar++;
        }



        // Funcion para reservar el byte de informacion
        void reservarByteParaElTotalDeInformacion ()
        {
            // Se deja libre el byte de bytes de informacion, que se completa luego
            tramaParaEnviar[totalDeBytesParaEnviar] = 0;

            // Se incrementa el contador de parametros utilizados
            totalDeBytesParaEnviar++;
        }



        // Funcion para agregar un dato booleano. Solo se puede poner 1 o 0, segun el ingreso
        void agregarInformacionBooleanaParaEnviar ( bool dato )
        {
            // Se envia el dato suministrado
            if (dato == true)
            {
                // Si es TRUE, se envia 1
                tramaParaEnviar[totalDeBytesParaEnviar] = 1;
            }
            else
            {
                // Si es FALSE, se envia 0
                tramaParaEnviar[totalDeBytesParaEnviar] = 0;
            }

            // Se incrementa el contador de parametros utilizados
            totalDeBytesParaEnviar++;
        }



        // Funcion para agregar un dato entero de solo 1 byte
        void agregarInformacionIntParaEnviar(decimal dato)
        {
            // Se castea el dato y se lo agrega
            tramaParaEnviar[totalDeBytesParaEnviar] = (byte) dato;

            // Se incrementa el contador de parametros utilizados
            totalDeBytesParaEnviar++;
        }


        // Funcion para actualizar el estado de los enables y los relays segun la terapia elegida
        private void cmbOpcionesDefault_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Se debe revisar el indice seleccionado
            switch( cmbOpcionesDefault.SelectedIndex )
            {
                // Se selecciono "CRIO_RF"
                case 0:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__CRIO_RF();
                    break;

                // Se selecciono "MONOP"
                case 1:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__MONOP();
                    break;

                // Se selecciono "VACIO"
                case 2:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__VACIO();
                    break;

                // Se selecciono "CR_MON"
                case 3:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__CR_MON();
                    break;

                // Se selecciono "FAC_ENYGMA_1T"
                case 4:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__FAC_ENYGMA_1T();
                    break;

                // Se selecciono "FAC_ENYGMA_2T"
                case 5:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__FAC_ENYGMA_2T();
                    break;

                // Se selecciono "VACIO_MON"
                case 6:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__VACIO_MON();
                    break;

                // Se selecciono "MON_DEL_VAC"
                case 7:
                    // Se configuran las opciones para esta terapia
                    ConfigurarOpcionesPara__MON_DEL_VAC();
                    break;
            }
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "CRIO_RF"
        private void ConfigurarOpcionesPara__CRIO_RF ()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = false;
            cbxEnable2.Checked = false;
            cbxEnable3.Checked = true;
            cbxEnable4.Checked = true;
            cbxEnable5.Checked = true;

            // Se setea el estado de los relays
            cbxRelay1.Checked = false;
            cbxRelay2.Checked = false;
            cbxRelay3.Checked = false;
            cbxRelay4.Checked = false;
            cbxRelay5.Checked = false;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "MONOP"
        private void ConfigurarOpcionesPara__MONOP()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = true;
            cbxEnable2.Checked = true;
            cbxEnable3.Checked = false;
            cbxEnable4.Checked = false;
            cbxEnable5.Checked = false;

            // Se setea el estado de los relays
            cbxRelay1.Checked = false;
            cbxRelay2.Checked = false;
            cbxRelay3.Checked = false;
            cbxRelay4.Checked = false;
            cbxRelay5.Checked = false;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "VACIO"
        private void ConfigurarOpcionesPara__VACIO()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = true;
            cbxEnable2.Checked = true;
            cbxEnable3.Checked = false;
            cbxEnable4.Checked = false;
            cbxEnable5.Checked = false;

            // Se setea el estado de los relays
            cbxRelay1.Checked = true;
            cbxRelay2.Checked = false;
            cbxRelay3.Checked = false;
            cbxRelay4.Checked = false;
            cbxRelay5.Checked = false;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "CR_MON"
        private void ConfigurarOpcionesPara__CR_MON()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = true;
            cbxEnable2.Checked = true;
            cbxEnable3.Checked = true;
            cbxEnable4.Checked = true;
            cbxEnable5.Checked = true;

            // Se setea el estado de los relays
            cbxRelay1.Checked = false;
            cbxRelay2.Checked = false;
            cbxRelay3.Checked = false;
            cbxRelay4.Checked = false;
            cbxRelay5.Checked = false;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "FAC_ENYGMA_1T"
        private void ConfigurarOpcionesPara__FAC_ENYGMA_1T()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = false;
            cbxEnable2.Checked = false;
            cbxEnable3.Checked = false;
            cbxEnable4.Checked = true;
            cbxEnable5.Checked = false;

            // Se setea el estado de los relays
            cbxRelay1.Checked = false;
            cbxRelay2.Checked = true;
            cbxRelay3.Checked = true;
            cbxRelay4.Checked = false;
            cbxRelay5.Checked = false;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "FAC_ENYGMA_2T"
        private void ConfigurarOpcionesPara__FAC_ENYGMA_2T()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = false;
            cbxEnable2.Checked = false;
            cbxEnable3.Checked = true;
            cbxEnable4.Checked = true;
            cbxEnable5.Checked = false;

            // Se setea el estado de los relays
            cbxRelay1.Checked = false;
            cbxRelay2.Checked = true;
            cbxRelay3.Checked = true;
            cbxRelay4.Checked = false;
            cbxRelay5.Checked = false;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "VACIO_MON"
        private void ConfigurarOpcionesPara__VACIO_MON()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = true;
            cbxEnable2.Checked = true;
            cbxEnable3.Checked = false;
            cbxEnable4.Checked = true;
            cbxEnable5.Checked = true;

            // Se setea el estado de los relays
            cbxRelay1.Checked = true;
            cbxRelay2.Checked = false;
            cbxRelay3.Checked = true;
            cbxRelay4.Checked = true;
            cbxRelay5.Checked = true;
        }



        // Funcion auxiliar para marcar los enables y relays para la opcion de "MON_DEL_VAC"
        private void ConfigurarOpcionesPara__MON_DEL_VAC()
        {
            // Se setea el estado de los enables
            cbxEnable1.Checked = false;
            cbxEnable2.Checked = false;
            cbxEnable3.Checked = false;
            cbxEnable4.Checked = true;
            cbxEnable5.Checked = true;

            // Se setea el estado de los relays
            cbxRelay1.Checked = false;
            cbxRelay2.Checked = false;
            cbxRelay3.Checked = true;
            cbxRelay4.Checked = true;
            cbxRelay5.Checked = true;
        }



        // Funcion para obtener el valor de temperatura segun la medicion del termistor
        bool CalcularTemperaturaDelTermistor ( byte termistor )
        {
            // Se declara una variable auxiliar, para calcular la resistencia del termistor
            double resistenciaAuxiliar;

            // Se definen variables auxiliares, para poder seguir todos los pasos de las ecuaciones
            double denominador;
            double numerador;

            // Se define una variable auxiliar, para almacenar el logaritmo natural de la resistencia obtenida
            double logaritmo;

            // Se declara una variable auxiliar, para almacenar el valor de la temperatura medida
            double temperaturaAuxiliar;

            // Se declara una variable auxiliar, para almacenar el valor de cuentas del termistor
            double cuentasDelTermistor;



            // Se debe verificar que se haya sumistrado un valor correcto para el calculo
            switch ( termistor )
            {
                // Se quiere obtener la temperatura que este midiendo el termistor 1
                case 1:

                    // Se actualiza el valor de las cuentas del termistor para los calculos
                    cuentasDelTermistor = THERM1;

                    break;


                // Se quiere obtener la temperatura que este midiendo el termistor 2
                case 2:

                    // Se actualiza el valor de las cuentas del termistor para los calculos
                    cuentasDelTermistor = THERM2;

                    break;


                // En el caso de un parametro incorrecto, simplemente se retorna de la funcion
                default:

                    return( false );
            }



            // ---------------------------- Se calcula la resistencia del termistor ---------------------------- //

            // Se calcula el numerador
            numerador = maximasCuentasDelADC / cuentasDelTermistor;
            numerador -= 1;
            numerador *= resistenciaA_2550;
            numerador *= resistenciaB_5600;

            // Se calcula el denominador
            denominador = maximasCuentasDelADC / cuentasDelTermistor;
            denominador -= 1;
            denominador *= resistenciaA_2550;
            denominador *= -1;
            denominador += resistenciaB_5600;

            // Se obtiene la resistencia del termistor
            resistenciaAuxiliar = numerador / denominador;



            // ---------------------------- Se calcula la temperatura del termistor ---------------------------- //

            // Se calcula el logaritmo de la resistencia medida
            logaritmo = Math.Log(resistenciaAuxiliar);

            // Se calcula el denominador
            denominador  = COEFICIENTE_A;
            denominador += COEFICIENTE_B * logaritmo;
            denominador += COEFICIENTE_C * logaritmo * logaritmo;
            denominador += COEFICIENTE_D * logaritmo * logaritmo * logaritmo;

            // Se calcula la temperatura
            temperaturaAuxiliar  = 1 / denominador;
            temperaturaAuxiliar -= KELVIN_A_CELCIUS;



            // ---------------------------- Se actualizan los valores calculados ---------------------------- //

            switch (termistor)
            {
                // Se quiere obtener la temperatura que este midiendo el termistor 1
                case 1:

                    // Se actualiza el valor de las cuentas del termistor para los calculos
                    TemperaturaDelTermistor1 = temperaturaAuxiliar;

                    break;


                // Se quiere obtener la temperatura que este midiendo el termistor 2
                case 2:

                    // Se actualiza el valor de las cuentas del termistor para los calculos
                    TemperaturaDelTermistor2 = temperaturaAuxiliar;

                    break;
            }

            return (true);
        }



        // Se deben reiniciar todos los valores caracteristicos del termistor 1
        private void btnReiniciarValoresCaracteristicos_Click(object sender, EventArgs e)
        {
            // Se reinician los valores caracteristicos del termistor 1
            THERM1_Maximo = 0;
            THERM1_Minimo = 0;
            THERM1_Promedio = 0;
            THERM1_Acumulado = 0;

            // Se actualiza la indicacion en pantalla para el parametro "Maximo del termistor 1"
            lblMaxTherm1.Text = THERM1_Maximo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Minimo del termistor 1"
            lblMinTherm1.Text = THERM1_Minimo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Promedio del termistor 1"
            lblPromTherm1.Text = THERM1_Promedio.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Mediciones del termistor 1"
            lblMedicionesTherm1.Text = totalDeMedicionesDelTermistor1.ToString("N0");
            

            // Se reinicia el contador de mediciones para el promediado del termistor 1
            totalDeMedicionesDelTermistor1 = 0;
        }



        // Se deben reiniciar todos los valores caracteristicos del termistor 2
        private void btnReiniciarValoresCaracteristicosTerm2_Click(object sender, EventArgs e)
        {
            // Se reinician los valores caracteristicos del termistor 2
            THERM2_Maximo = 0;
            THERM2_Minimo = 0;
            THERM2_Promedio = 0;
            THERM2_Acumulado = 0;

            // Se actualiza la indicacion en pantalla para el parametro "Maximo del termistor 2"
            lblMaxTherm2.Text = THERM2_Maximo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Minimo del termistor 2"
            lblMinTherm2.Text = THERM2_Minimo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Promedio del termistor 2"
            lblPromTherm2.Text = THERM2_Promedio.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Mediciones del termistor 2"
            lblMedicionesTherm2.Text = totalDeMedicionesDelTermistor2.ToString("N0");

            // Se reinicia el contador de mediciones para el promediado del termistor 2
            totalDeMedicionesDelTermistor2 = 0;
        }



        // Se deben reiniciar todos los valores caracteristicos de ambos termistores
        private void btnReiniciarValoresCaracteristicosAmbosTerm_Click(object sender, EventArgs e)
        {
            // Se reinician los valores caracteristicos del termistor 1
            THERM1_Maximo = 0;
            THERM1_Minimo = 0;
            THERM1_Promedio = 0;
            THERM1_Acumulado = 0;

            // Se actualiza la indicacion en pantalla para el parametro "Maximo del termistor 1"
            lblMaxTherm1.Text = THERM1_Maximo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Minimo del termistor 1"
            lblMinTherm1.Text = THERM1_Minimo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Promedio del termistor 1"
            lblPromTherm1.Text = THERM1_Promedio.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Mediciones del termistor 1"
            lblMedicionesTherm1.Text = totalDeMedicionesDelTermistor1.ToString("N0");

            // Se reinicia el contador de mediciones para el promediado del termistor 1
            totalDeMedicionesDelTermistor1 = 0;


            // Se reinician los valores caracteristicos del termistor 2
            THERM2_Maximo = 0;
            THERM2_Minimo = 0;
            THERM2_Promedio = 0;
            THERM2_Acumulado = 0;

            // Se actualiza la indicacion en pantalla para el parametro "Maximo del termistor 2"
            lblMaxTherm2.Text = THERM2_Maximo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Minimo del termistor 2"
            lblMinTherm2.Text = THERM2_Minimo.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Promedio del termistor 2"
            lblPromTherm2.Text = THERM2_Promedio.ToString("N0");

            // Se actualiza la indicacion en pantalla para el parametro "Mediciones del termistor 2"
            lblMedicionesTherm2.Text = totalDeMedicionesDelTermistor2.ToString("N0");

            // Se reinicia el contador de mediciones para el promediado del termistor 2
            totalDeMedicionesDelTermistor2 = 0;
        }










        private void cbxLed1_CheckedChanged(object sender, EventArgs e)
        {
            // Se envia el comando para actualizar la configuracion
            EnviarComando( (byte) comandos_enviados.COMANDO_ENVIADO__ACTUALIZAR_LED );
        }
    }

}