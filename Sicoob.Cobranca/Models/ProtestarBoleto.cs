﻿namespace Sicoob.Cobranca.Models;

public class ProtestarBoletoRequest: BoletoBase
{
    public int? numeroContratoCobranca { get; set; }
}

public class ProtestarBoletoResponse
{
    public DadosBaixa[]? resultado { get; set; }
    public ResultadoInfo? mensagens { get; set; }
}
public class DadosProtestar
{
    public ResultadoInfo? status { get; set; }
    public ProtestarBoletoRequest? boleto { get; set; }
}