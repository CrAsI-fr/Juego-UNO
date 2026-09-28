namespace Juego_UNO
{
    partial class FormElegirColor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.BotonRojo = new System.Windows.Forms.Button();
            this.BotonAzul = new System.Windows.Forms.Button();
            this.BotonVerde = new System.Windows.Forms.Button();
            this.BotonAmarillo = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // BotonRojo
            // 
            this.BotonRojo.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.BotonRojo.Location = new System.Drawing.Point(12, 12);
            this.BotonRojo.Name = "BotonRojo";
            this.BotonRojo.Size = new System.Drawing.Size(383, 196);
            this.BotonRojo.TabIndex = 0;
            this.BotonRojo.Text = "ROJO";
            this.BotonRojo.UseVisualStyleBackColor = true;
            this.BotonRojo.Click += new System.EventHandler(this.BotonRojo_Click);
            // 
            // BotonAzul
            // 
            this.BotonAzul.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.BotonAzul.Location = new System.Drawing.Point(12, 215);
            this.BotonAzul.Name = "BotonAzul";
            this.BotonAzul.Size = new System.Drawing.Size(383, 205);
            this.BotonAzul.TabIndex = 1;
            this.BotonAzul.Text = "AZUL";
            this.BotonAzul.UseVisualStyleBackColor = true;
            this.BotonAzul.Click += new System.EventHandler(this.BotonAzul_Click);
            // 
            // BotonVerde
            // 
            this.BotonVerde.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.BotonVerde.Location = new System.Drawing.Point(401, 12);
            this.BotonVerde.Name = "BotonVerde";
            this.BotonVerde.Size = new System.Drawing.Size(387, 196);
            this.BotonVerde.TabIndex = 2;
            this.BotonVerde.Text = "VERDE";
            this.BotonVerde.UseVisualStyleBackColor = true;
            this.BotonVerde.Click += new System.EventHandler(this.button3_Click);
            // 
            // BotonAmarillo
            // 
            this.BotonAmarillo.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.BotonAmarillo.Location = new System.Drawing.Point(401, 214);
            this.BotonAmarillo.Name = "BotonAmarillo";
            this.BotonAmarillo.Size = new System.Drawing.Size(387, 206);
            this.BotonAmarillo.TabIndex = 3;
            this.BotonAmarillo.Text = "AMARILLO";
            this.BotonAmarillo.UseVisualStyleBackColor = true;
            this.BotonAmarillo.Click += new System.EventHandler(this.BotonAmarillo_Click);
            // 
            // FormElegirColor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.ControlBox = false;
            this.Controls.Add(this.BotonAmarillo);
            this.Controls.Add(this.BotonVerde);
            this.Controls.Add(this.BotonAzul);
            this.Controls.Add(this.BotonRojo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormElegirColor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormElegirColor";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button BotonRojo;
        private System.Windows.Forms.Button BotonAzul;
        private System.Windows.Forms.Button BotonVerde;
        private System.Windows.Forms.Button BotonAmarillo;
    }
}