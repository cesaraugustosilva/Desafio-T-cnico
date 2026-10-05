# Desafio Técnico em C#

Solução de três exercícios desenvolvidos em C#:

1. Cálculo de comissão de vendedores a partir de um arquivo JSON.
2. Controle de entrada e saída de produtos no estoque.
3. Cálculo de juros de 2,5% ao dia sobre valores vencidos.

## Arquivos

- `desafio.cs`: código principal.
- `vendas.json`: registros de vendas.
- `estoque.json`: produtos e quantidades em estoque.
- `movimentacoes.json`: gerado durante a execução para guardar o histórico.

## Executar

Compile o programa:

```powershell
csc /r:System.Web.Extensions.dll desafio.cs
