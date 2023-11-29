using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    // Esta classe trata as conexões. Serão tantas quano as instâncias dos usuários conectados
    class Conexao
    {
        TcpClient tcpCliente;

        // A thread que irá enviar a informação para o cliente
        private Thread thrSender;
        private StreamReader srReceptor;
        private StreamWriter swRemetente;
        private string usuarioAtual;
        private string strResposta;

        // O construtor da classe que toma a conexão TCP
        public Conexao(TcpClient tcpCon)
        {
            tcpCliente = tcpCon;
            // A thread que aceita o cliente e espera a mensagem
            thrSender = new Thread(AceitaCliente);
            thrSender.IsBackground = true; // isBackground encerra esse processo ao encerrar a aplicação
            // A thread chama o método AceitaCliente()
            thrSender.Start();
        }

        private void FechaConexao()
        {
            // Fecha os objetos abertos
            tcpCliente.Close();
            srReceptor.Close();
            swRemetente.Close();
        }

        // Ocorre quando um novo cliente é aceito
        private void AceitaCliente()
        {
            srReceptor = new StreamReader(tcpCliente.GetStream());
            swRemetente = new StreamWriter(tcpCliente.GetStream());

            // Lê a informação da conta do cliente
            usuarioAtual = srReceptor.ReadLine();

            // temos uma resposta do cliente
            if(usuarioAtual != "")
            {
                // Armazena o nome de usuario na hash table
                if(Servidor.htUsuarios.Contains(usuarioAtual)) 
                {
                    // 0 => significa não conectado
                    swRemetente.WriteLine("0 | Este nome de usuário já existe.");
                    swRemetente.Flush();
                } 
                else if (usuarioAtual == "Administrador")
                {
                    // 0 => não conectado
                    swRemetente.WriteLine("0 | Este nome de usuário é reservado.");
                    swRemetente.Flush();
                    return;
                }
                else
                {
                    // 1 => conectou com sucesso
                    swRemetente.WriteLine("1");
                    swRemetente.Flush();

                    // Inclui o usuário na has table e inicia a escuta de suas mensagens
                    Servidor.IncluiUsuario(tcpCliente, usuarioAtual);
                }
            }
            else
            {
                FechaConexao();
                return;
            }

            try
            {
                // Continua aguardando por uma mensagem de usuário
                // lê o string.ReadLine() em strResposta e vê se é diferente de vazio
                while ((strResposta = srReceptor.ReadLine()) != "") 
                {
                    // Se for inválido, remove-o
                    if(strResposta == null)
                    {
                        Servidor.RemoveUsuario(tcpCliente);
                    }
                    else
                    {
                        // Envia a msg para todos os demais usuários
                        Servidor.EnviaMensagem(usuarioAtual, strResposta);
                    }
                }
            }
            catch
            {
                // Se houver um problema com este usuário, desconecta-o
                Servidor.RemoveUsuario(tcpCliente);
            }
        }
    }
}
