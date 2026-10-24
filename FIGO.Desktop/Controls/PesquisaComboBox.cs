using System.ComponentModel;
using FIGO.backend;

namespace FIGO.Desktop;

internal sealed record ItemSelecao(int Id, string Nome, string Busca = "")
{
    public override string ToString() => Nome;
}

// Busca por trecho em qualquer parte do nome/documento, sem diferenciar acentos.
// Digitar texto não confirma uma opção: é preciso selecioná-la na lista.
internal sealed class PesquisaComboBox : ComboBox
{
    private List<ItemSelecao> opcoes = [];
    private ItemSelecao? escolha;
    private bool atualizando;
    public PesquisaComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDown;
        AutoCompleteMode = AutoCompleteMode.None;
        IntegralHeight = false;
        MaxDropDownItems = 8;
        DropDownHeight = 220;
        Font = UiTheme.Font(10);
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? IdSelecionado => escolha is not null && Text == escolha.Nome ? escolha.Id : null;

    public void DefinirOpcoes(IEnumerable<ItemSelecao> itens, int? id = null)
    {
        opcoes = itens.ToList();
        Escolher(id);
    }
    public void Escolher(int? id)
    {
        atualizando = true;
        try
        {
            escolha = opcoes.FirstOrDefault(o => o.Id == id);
            BeginUpdate(); Items.Clear(); Items.AddRange(opcoes.Cast<object>().ToArray()); EndUpdate();
            SelectedItem = escolha;
            Text = escolha?.Nome ?? "";
            SelectionStart = Text.Length; SelectionLength = 0;
        }
        finally { atualizando = false; }
    }
    // Também usada pelos testes do controle com entradas de teclado simuladas.
    internal void Pesquisar(string busca)
    {
        atualizando = true;
        try
        {
            escolha = null;
            int posicao = Math.Min(SelectionStart, busca.Length);
            BeginUpdate();
            Items.Clear();
            Items.AddRange(opcoes.Where(o => BuscaFinanceira.Corresponde(o.Nome + " " + o.Busca, busca)).Cast<object>().ToArray());
            SelectedIndex = -1;
            EndUpdate();
            Text = busca; SelectionStart = posicao; SelectionLength = 0;
        }
        finally { atualizando = false; }
    }
    protected override void OnTextUpdate(EventArgs e)
    {
        base.OnTextUpdate(e);
        if (atualizando) return;
        string busca = Text;
        int posicao = SelectionStart;
        Pesquisar(busca);
        if (Focused && Items.Count > 0) DroppedDown = true;
        // Abrir o dropdown nativo pode selecionar o texto: restaurar o cursor.
        SelectionStart = Math.Min(posicao, Text.Length); SelectionLength = 0;
        Cursor.Current = Cursors.IBeam;
    }
    protected override void OnTextChanged(EventArgs e)
    {
        if (!atualizando && escolha is not null && Text != escolha.Nome) escolha = null;
        base.OnTextChanged(e);
    }
    protected override void OnDropDown(EventArgs e)
    {
        if (IdSelecionado is not null)
        {
            int? id = IdSelecionado;
            Escolher(id); // Uma seleção confirmada reabre a lista completa.
        }
        base.OnDropDown(e);
    }
    protected override void OnSelectionChangeCommitted(EventArgs e)
    {
        if (SelectedItem is ItemSelecao item) escolha = item;
        base.OnSelectionChangeCommitted(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && DroppedDown)
        {
            if (SelectedItem is ItemSelecao item) { escolha = item; Text = item.Nome; }
            DroppedDown = false;
            e.Handled = e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape && DroppedDown)
        {
            DroppedDown = false;
            e.Handled = e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }
}
