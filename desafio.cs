using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

class Venda
{
    public string vendedor { get; set; }
    public decimal valor { get; set; }
}

class DadosVendas
{
    public List<Venda> vendas { get; set; }
}

class Produto
{
    public int codigoProduto { get; set; }
    public string descricaoProduto { get; set; }
    public int estoque { get; set; }
}

class DadosEstoque
{
    public List<Produto> estoque { get; set; }
}

class Movimentacao
{
    public int Id { get; set; }
    public int CodigoProduto { get; set; }
    public string Tipo { get; set; }
    public string Descricao { get; set; }
    public int Quantidade { get; set; }
    public DateTime Data { get; set; }
}

class Program
{
    private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        while (true)
        {
            if (!Console.IsOutputRedirected)
                Console.Clear();
            Console.WriteLine("DESAFIO TÉCNICO");
            Console.WriteLine("1 - Calcular comissões");
            Console.WriteLine("2 - Movimentar estoque");
            Console.WriteLine("3 - Calcular juros por atraso");
            Console.WriteLine("0 - Sair");
            Console.Write("Escolha uma opção: ");

            string opcao = Console.ReadLine();
            Console.WriteLine();

            try
            {
                if (opcao == "1")
                    CalcularComissoes();
                else if (opcao == "2")
                    MovimentarEstoque();
                else if (opcao == "3")
                    CalcularJuros();
                else if (opcao == "0")
                    return;
                else
                    Console.WriteLine("Opção inválida.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }

            Console.WriteLine("\nPressione ENTER para voltar ao menu.");
            Console.ReadLine();
        }
    }

    private static string CaminhoArquivo(string nome)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nome);
    }

    private static void CalcularComissoes()
    {
        string json = File.ReadAllText(CaminhoArquivo("vendas.json"), Encoding.UTF8);
        DadosVendas dados = Json.Deserialize<DadosVendas>(json);

        if (dados == null || dados.vendas == null)
            throw new InvalidDataException("O arquivo vendas.json não possui dados válidos.");

        var resultado = dados.vendas
            .GroupBy(venda => venda.vendedor)
            .Select(grupo => new
            {
                Vendedor = grupo.Key,
                TotalVendido = grupo.Sum(venda => venda.valor),
                Comissao = grupo.Sum(venda => CalcularComissaoDaVenda(venda.valor))
            });

        Console.WriteLine("COMISSÃO POR VENDEDOR\n");
        foreach (var item in resultado)
        {
            Console.WriteLine("Vendedor: {0}", item.Vendedor);
            Console.WriteLine("Total vendido: {0}", item.TotalVendido.ToString("C", Cultura));
            Console.WriteLine("Comissão: {0}\n", item.Comissao.ToString("C", Cultura));
        }
    }

    private static decimal CalcularComissaoDaVenda(decimal valor)
    {
        if (valor < 100m)
            return 0m;
        if (valor < 500m)
            return valor * 0.01m;
        return valor * 0.05m;
    }

    private static void MovimentarEstoque()
    {
        string caminho = CaminhoArquivo("estoque.json");
        DadosEstoque dados = Json.Deserialize<DadosEstoque>(
            File.ReadAllText(caminho, Encoding.UTF8));

        if (dados == null || dados.estoque == null)
            throw new InvalidDataException("O arquivo estoque.json não possui dados válidos.");

        string caminhoMovimentacoes = CaminhoArquivo("movimentacoes.json");
        List<Movimentacao> historico = File.Exists(caminhoMovimentacoes)
            ? Json.Deserialize<List<Movimentacao>>(File.ReadAllText(caminhoMovimentacoes, Encoding.UTF8))
            : new List<Movimentacao>();
        if (historico == null)
            historico = new List<Movimentacao>();

        int proximoId = historico.Count == 0 ? 1 : historico.Max(item => item.Id) + 1;
        List<Movimentacao> movimentacoesDaSessao = new List<Movimentacao>();

        while (true)
        {
            Console.WriteLine("ESTOQUE ATUAL");
            foreach (Produto produto in dados.estoque)
                Console.WriteLine("{0} - {1}: {2} unidade(s)",
                    produto.codigoProduto, produto.descricaoProduto, produto.estoque);

            Console.Write("\nCódigo do produto (0 para encerrar): ");
            int codigo = LerInteiro();
            if (codigo == 0)
                break;

            Produto selecionado = dados.estoque.FirstOrDefault(
                produto => produto.codigoProduto == codigo);
            if (selecionado == null)
            {
                Console.WriteLine("Produto não encontrado.\n");
                continue;
            }

            Console.Write("Tipo (E para entrada / S para saída): ");
            string tipo = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();
            if (tipo != "E" && tipo != "S")
            {
                Console.WriteLine("Tipo de movimentação inválido.\n");
                continue;
            }

            Console.Write("Quantidade: ");
            int quantidade = LerInteiro();
            if (quantidade <= 0)
            {
                Console.WriteLine("A quantidade precisa ser maior que zero.\n");
                continue;
            }

            if (tipo == "S" && quantidade > selecionado.estoque)
            {
                Console.WriteLine("Saída negada: estoque insuficiente.\n");
                continue;
            }

            Console.Write("Descrição da movimentação: ");
            string descricao = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(descricao))
                descricao = tipo == "E" ? "Entrada de mercadoria" : "Saída de mercadoria";

            selecionado.estoque += tipo == "E" ? quantidade : -quantidade;
            Movimentacao movimentacao = new Movimentacao
            {
                Id = proximoId++,
                CodigoProduto = codigo,
                Tipo = tipo == "E" ? "Entrada" : "Saída",
                Descricao = descricao,
                Quantidade = quantidade,
                Data = DateTime.Now
            };
            historico.Add(movimentacao);
            movimentacoesDaSessao.Add(movimentacao);

            File.WriteAllText(caminho, Json.Serialize(dados), Encoding.UTF8);
            File.WriteAllText(caminhoMovimentacoes, Json.Serialize(historico), Encoding.UTF8);

            Console.WriteLine("\nMovimentação nº {0} registrada: {1}.",
                movimentacao.Id, movimentacao.Descricao);
            Console.WriteLine("Estoque final de {0}: {1} unidade(s).\n",
                selecionado.descricaoProduto, selecionado.estoque);
        }

        if (movimentacoesDaSessao.Count > 0)
        {
            Console.WriteLine("\nMOVIMENTAÇÕES DESTA EXECUÇÃO");
            foreach (Movimentacao item in movimentacoesDaSessao)
                Console.WriteLine("#{0} | {1} | Produto {2} | {3}: {4} unidade(s)",
                    item.Id, item.Tipo, item.CodigoProduto, item.Descricao, item.Quantidade);
        }
    }

    private static int LerInteiro()
    {
        int valor;
        return int.TryParse(Console.ReadLine(), out valor) ? valor : -1;
    }

    private static void CalcularJuros()
    {
        decimal valor;
        Console.Write("Valor original: R$ ");
        if (!decimal.TryParse(Console.ReadLine(), NumberStyles.Number, Cultura, out valor) || valor < 0)
        {
            Console.WriteLine("Valor inválido.");
            return;
        }

        Console.Write("Data de vencimento (dd/MM/aaaa): ");
        DateTime vencimento;
        if (!DateTime.TryParseExact(Console.ReadLine(), "dd/MM/yyyy", Cultura,
            DateTimeStyles.None, out vencimento))
        {
            Console.WriteLine("Data inválida.");
            return;
        }

        int diasAtraso = Math.Max(0, (DateTime.Today - vencimento.Date).Days);
        decimal juros = valor * 0.025m * diasAtraso;
        decimal total = valor + juros;

        Console.WriteLine("\nData do cálculo: {0}", DateTime.Today.ToString("dd/MM/yyyy"));
        Console.WriteLine("Dias de atraso: {0}", diasAtraso);
        Console.WriteLine("Juros (2,5% ao dia): {0}", juros.ToString("C", Cultura));
        Console.WriteLine("Valor total: {0}", total.ToString("C", Cultura));
    }
}
