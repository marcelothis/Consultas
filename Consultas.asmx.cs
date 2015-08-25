using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;

namespace Servicos
{
    /// <summary>
    /// Summary description for Consultas
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Consultas : System.Web.Services.WebService
    {

        //Exemplos de querys para requisição no servidor do DETRAN
        string url = "http://www2.detran.rn.gov.br/servicos/consultaveiculo.asp";
        string query = "placa=myy6978&renavam=5566778986";
        //string query = "placa=" + placa + "&renavam=" + renavam + "&btnConsultaPlaca=";
        //Primeira função a ser chamada...

        [WebMethod]
        public string consultas(string entrada)
        {

            List<Veiculo> resultado = new List<Veiculo>();

            List<Veiculo> listVeiculos = JsonConvert.DeserializeObject<List<Veiculo>>(entrada);

            for (int i = 0; i < listVeiculos.Count; i++)
            {
                var consulta = consultaVeicular(url, "placa=" + listVeiculos[i].placa + "&renavam=" + listVeiculos[i].renavam + "&btnConsultaPlaca=");

                if (consulta == "comRestricao")
                {
                    var retorno = "{\"placa\":\"" + listVeiculos[i].placa + "\", \"renavam\":\"" + listVeiculos[i].renavam + "\", \"status\":\"comRestricao\"}";
                    Veiculo v;
                    v = JsonConvert.DeserializeObject<Veiculo>(retorno);
                    //resultado.Add(listVeiculos[i]);
                    resultado.Add(v);
                }
                if(consulta == "semRestricao")
                {
                    var retorno = "{\"placa\":\"" + listVeiculos[i].placa + "\", \"renavam\":\"" + listVeiculos[i].renavam + "\", \"status\":\"semRestricao\"}";
                    Veiculo v;
                    v = JsonConvert.DeserializeObject<Veiculo>(retorno);
                    //resultado.Add(listVeiculos[i]);
                    resultado.Add(v);
                }
                if (consulta == "naoExiste")
                {
                    var retorno = "{\"placa\":\"" + listVeiculos[i].placa + "\", \"renavam\":\"" + listVeiculos[i].renavam + "\", \"status\":\"naoExiste\"}";
                    Veiculo v;
                    v = JsonConvert.DeserializeObject<Veiculo>(retorno);
                    //resultado.Add(listVeiculos[i]);
                    resultado.Add(v);
                }

            }

            var resultadoJson = JsonConvert.SerializeObject(resultado);
            return resultadoJson;

        }

        private string consultaVeicular(string url, string query)
        {
            // Declarações necessárias
            Stream requestStream = null;
            WebResponse response = null;
            StreamReader reader = null;


            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Post;

                // Neste ponto, você está setando a propriedade ContentType da página
                // para urlencoded para que o comando POST seja enviado corretamente
                request.ContentType = "application/x-www-form-urlencoded";

                StringBuilder urlEncoded = new StringBuilder();

                // Separando cada parâmetro
                Char[] reserved = { '?', '=' };

                // alocando o bytebuffer
                byte[] byteBuffer = null;

                // caso a URL seja preenchida
                if (query != null)
                {
                    int i = 0, j;
                    // percorre cada caractere da url atraz das palavras reservadas para separação
                    // dos parâmetros
                    while (i < query.Length)
                    {
                        j = query.IndexOfAny(reserved, i);
                        if (j == -1)
                        {
                            urlEncoded.Append(query.Substring(i, query.Length - i));
                            break;
                        }
                        urlEncoded.Append(query.Substring(i, j - i));
                        urlEncoded.Append(query.Substring(j, 1));
                        i = j + 1;
                    }
                    // codificando em UTF8 (evita que sejam mostrados códigos malucos em caracteres especiais
                    byteBuffer = Encoding.UTF8.GetBytes(urlEncoded.ToString());

                    request.ContentLength = byteBuffer.Length;
                    requestStream = request.GetRequestStream();
                    requestStream.Write(byteBuffer, 0, byteBuffer.Length);
                    requestStream.Close();
                }
                else
                {
                    request.ContentLength = 0;
                }

                // Dados recebidos
                response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                // Codifica os caracteres especiais para que possam ser exibidos corretamente
                System.Text.Encoding encoding = System.Text.Encoding.Default;

                // Preenche o reader
                reader = new StreamReader(responseStream, encoding);

                Char[] charBuffer = new Char[256];
                int count = reader.Read(charBuffer, 0, charBuffer.Length);

                StringBuilder Dados = new StringBuilder();

                // Lê cada byte para preencher meu stringbuilder
                while (count > 0)
                {
                    Dados.Append(new String(charBuffer, 0, count));
                    count = reader.Read(charBuffer, 0, charBuffer.Length);
                }

                List<string> retorno;
                retorno = extrairResultadoVeiculoDetranRN(Dados.ToString());

                //Se vier com resultado o retorno da consulta no DETRAN tem que ser maior que 0...
                if (retorno.Count > 0)
                    if (retorno[10] == "" || retorno[9] == "" || retorno[8] == "" || retorno[7] == "")
                    {
                        return "semRestricao";
                    }
                    else
                    {
                        return "comRestricao";
                    }
                else
                {
                    return "naoExiste";
                }

            }
            catch (Exception e)
            {
                // Ocorreu algum erro
                //Console.Write("Erro: " + e.Message);
                //return retorno;
            }
            finally
            {
                // Fecha tudo
                if (requestStream != null)
                    requestStream.Close();
                if (response != null)
                    response.Close();
                if (reader != null)
                    reader.Close();
            }
            return "naoExiste";
        }

        //Função para retirar o HTML e deixar só o filé...
        private List<string> extrairResultadoVeiculoDetranRN(string dados)
        {

            var tipo = System.Text.RegularExpressions.Regex.Match(dados, "tipo<br><span class=\"celnlef\" >(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var marca = Regex.Match(dados, "marca/modelo<br><span class=\"celnlef\"  >(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var nomeProprietario = Regex.Match(dados, "nome do proprietário<br><span class=\"celnlef\" >(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var licenciadoAte = Regex.Match(dados, "licenciado até<br><span class=\"celnlef\" >(.+?),", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var cor = Regex.Match(dados, "cor<br><span class=\"celnlef\" >(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var situacao = Regex.Match(dados, "situação<br><span class=\"celnlef\" >(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var ano = Regex.Match(dados, "fabricação/modelo<br><span class=\"celnlef\">(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);


            //Dados de multas e IPVA
            var multa = Regex.Match(dados, "multas<br><span class=\"celrig\">(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var ipva = Regex.Match(dados, "ipva<br><span class=\"celrig\">(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var seguroDPVAT = Regex.Match(dados, "seguro dpvat<br><span class=\"celrig\">(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var taxasDetran = Regex.Match(dados, "taxas detran<br><span class=\"celrig\">(.+?)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);


            List<string> retorno = new List<string>();
            if (tipo.Success)
            {
                retorno.Add(tipo.Groups[1].Value);
                retorno.Add(marca.Groups[1].Value);
                retorno.Add(nomeProprietario.Groups[1].Value);
                retorno.Add(licenciadoAte.Groups[1].Value);
                retorno.Add(cor.Groups[1].Value);
                retorno.Add(situacao.Groups[1].Value);
                retorno.Add(ano.Groups[1].Value);
                retorno.Add(multa.Groups[1].Value);
                retorno.Add(ipva.Groups[1].Value);
                retorno.Add(seguroDPVAT.Groups[1].Value);
                retorno.Add(taxasDetran.Groups[1].Value);
            }

            return retorno;
        }


    }
}
