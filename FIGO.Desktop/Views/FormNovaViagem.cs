using FIGO.backend;

namespace FIGO.Desktop
{
    public class FormNovaViagem : Form
    {
        private readonly TextBox txtNome = new() { Left = 140, Top = 20, Width = 260 };
        private readonly TextBox txtDestino = new() { Left = 140, Top = 55, Width = 260 };
        private readonly DateTimePicker dtInicio = new() { Left = 140, Top = 90, Width = 260, Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker dtFim = new() { Left = 140, Top = 125, Width = 260, Format = DateTimePickerFormat.Short };
        private readonly NumericUpDown numParticipantes = new() { Left = 140, Top = 160, Width = 260, Minimum = 0, Maximum = 500 };
        private readonly Label lblErro = new() { Left = 20, Top = 195, Width = 380, ForeColor = Color.Red, AutoSize = false };

        private readonly ViagemServices viagemService = new();

        public Viagem? ViagemCriada { get; private set; }

        public FormNovaViagem()
        {
            Text = "Nova viagem";
            Width = 440;
            Height = 300;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Nome", Left = 20, Top = 23, Width = 110 });
            Controls.Add(new Label { Text = "Destino", Left = 20, Top = 58, Width = 110 });
            Controls.Add(new Label { Text = "Data início", Left = 20, Top = 93, Width = 110 });
            Controls.Add(new Label { Text = "Data fim", Left = 20, Top = 128, Width = 110 });
            Controls.Add(new Label { Text = "Participantes", Left = 20, Top = 163, Width = 110 });

            Controls.Add(txtNome);
            Controls.Add(txtDestino);
            Controls.Add(dtInicio);
            Controls.Add(dtFim);
            Controls.Add(numParticipantes);
            Controls.Add(lblErro);

            var btnSalvar = new Button { Text = "Salvar", Left = 220, Top = 220, Width = 90 };
            var btnCancelar = new Button { Text = "Cancelar", Left = 320, Top = 220, Width = 90 };

            btnSalvar.Click += BtnSalvar_Click;
            btnCancelar.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(btnSalvar);
            Controls.Add(btnCancelar);
        }

        private void BtnSalvar_Click(object? sender, EventArgs e)
        {
            lblErro.Text = "";

            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                lblErro.Text = "Informe o nome da viagem.";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDestino.Text))
            {
                lblErro.Text = "Informe o destino.";
                return;
            }

            if (dtFim.Value.Date < dtInicio.Value.Date)
            {
                lblErro.Text = "A data fim não pode ser antes da data início.";
                return;
            }

            var viagem = new Viagem
            {
                Nome = txtNome.Text.Trim(),
                Destino = txtDestino.Text.Trim(),
                DataInicio = dtInicio.Value.Date,
                DataFim = dtFim.Value.Date,
                QuantidadeParticipantes = (int)numParticipantes.Value
            };

            try
            {
                viagemService.Adicionar(viagem);
            }
            catch (Exception ex)
            {
                lblErro.Text = "Erro ao salvar: " + ex.Message;
                return;
            }

            ViagemCriada = viagem;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
