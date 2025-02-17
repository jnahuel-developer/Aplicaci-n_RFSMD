using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

// Debe tener el mismo "namespace" que la solucion general
namespace DistribucionFuenteVariable
{
    public partial class PuertoSerie : Form
    {
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        /*                                                          VARIABLES                                                                       */
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/

        // Variable que debe devolver
        public string nombreDelPuerto = "";



        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        /*                                                  CONSTRUCTOR DEL FORMULARIO                                                              */
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/

        public PuertoSerie()
        {
            InitializeComponent();

            // Se centra el formulario en la pantalla
            this.StartPosition = FormStartPosition.CenterScreen;

            // Se quita la posibilidad al usuario de poder editar el texto referente al nombre del puerto, para evitar errores
            cmbPuertos.DropDownStyle = ComboBoxStyle.DropDownList;

            // Se listan los puertos disponibles
            try
            {
                foreach( string s in SerialPort.GetPortNames() ) { cmbPuertos.Items.Add(s); }
                if( cmbPuertos.Items.Count > 0 )
                    { cmbPuertos.Text = cmbPuertos.Items[0].ToString(); }
                else
                {
                    MessageBox.Show("No se detectaron puertos series en uso");
                    this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Se produjo un error al listar los puertos :" + ex.Message);
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            }
        }



        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/
        /*                                        FUNCIONES PARA ACEPTAR O CANCELAR EL FORMULARIO                                                   */
        /********************************************************************************************************************************************/
        /********************************************************************************************************************************************/

        // Funcion encargada de realizar las acciones para una correcta terminación del formulario
        private void terminarCorrectamente()
        {
            this.nombreDelPuerto = cmbPuertos.Items[ cmbPuertos.SelectedIndex ].ToString();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        // Funcion encargada de realizar las acciones para una incorrecta terminación del formulario
        private void terminarIncorrectamente()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        // Boton "Aceptar"
        private void btnAceptar_Click(object sender, EventArgs e) { terminarCorrectamente(); }

        // Boton "Cancelar"
        private void btnCancelar_Click(object sender, EventArgs e) { terminarIncorrectamente(); }

    }
}
