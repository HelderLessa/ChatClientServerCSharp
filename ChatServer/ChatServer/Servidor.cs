using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatServer
{
    // Este delegate é necessário para especificar os parâmetros que estamos passando com o nosso evento
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);
    internal class Servidor
    {
        // Esta hash armazena os usuários e as conexões (acessado/consultado por usuário)
        public static Hashtable htUsuarios = new Hashtable(30); // 30 usuários é o limite definido
        // Esta hash armazena os usuários e as conexões (acessada/consultada por conexões)
        public static Hashtable htConexoes = new Hashtable(30); // 30 conexões é o limite definido
        // armazena o endereço IP passado
        private IPAddress enderecoIP;

        private int portaHost;
        private TcpClient tcpCliente;

        // O evento e o seu argumento irá notificar o formulário quando um usuário se conecta, desconecta, envia msg, etc
        public static event StatusChangedEventHandler StatusChanged;
        private static StatusChangedEventArgs e;

        // O construtor define o endereço IP para aquele retornado pela instanciação do objeto
        public Servidor(IPAddress endereco, int porta)
        {
            enderecoIP = endereco;
            portaHost = porta;
        }

        // A thread que irá tratar o escutador de conexões
        private Thread thrListener;

        // O objeto TCP object que escuta as conexões
        private TcpListener tlsCliente;

        // Irá dizer ao laço while para manter a monitoração das conexões
        bool ServRodando = false;

        // Inclui o usuário nas tabelas hash
        public static void IncluiUsuario(TcpClient tcpUsuario, string strUsername)
        {
            // Primeiro inclui o nome (chave 1) e conexão (chave 2) associadas para ambas as hash tables
            Servidor.htUsuarios.Add(strUsername, tcpUsuario); // hashtable.Add("chave1", "valor1");
            Servidor.htConexoes.Add(tcpUsuario, strUsername); // hashtable.Add("chave1", "valor1");

            // Informa a nova conexão para todos os usuários e para o formulario do servidor
            EnviaMensagemAdmin(htConexoes[tcpUsuario] + " entrou..."); // acessando valor associado à chave 1: hashtable["chave1"]
        }

        // Remove o usuário das tabelas (hash tables)
        public static void RemoveUsuario(TcpClient tcpUsuario)
        {
            // Se o usuário existir
            if (htConexoes[tcpUsuario] != null)
            {
                // Primeiro mostra a informação e informa os outros usuários sobre a conexão
                EnviaMensagemAdmin(htConexoes[tcpUsuario] + " saiu...");

                // Depois remove o usuário da hash table
                Servidor.htUsuarios.Remove(Servidor.htConexoes[tcpUsuario]); // dessa forma o valor de htConexoes (strUsername) é acessado diretamente como chave em htUsuarios
                Servidor.htConexoes.Remove(tcpUsuario);
            }
        }

        // Este evento é chamado quando queremos disparar o evento StatusChanged
        public static void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChangedEventHandler statusHandler = StatusChanged;

            if(statusHandler != null)
            {
                // invoca o delegate
                statusHandler(null, e);
            }
        }

        // Envia mensagens administrativas
        public static void EnviaMensagemAdmin(string Mensagem)
        {
            StreamWriter swSenderSender;

            // Exibe primeiro na aplicação
            e = new StatusChangedEventArgs("Administrador: " + Mensagem);
            OnStatusChanged(e);

            // Cria um array de clientes TCPs com o tamanho do número de clientes existentes
            TcpClient[] tcpClientes = new TcpClient[Servidor.htUsuarios.Count];
            // Copia os objetos TcpCliente no array
            Servidor.htUsuarios.Values.CopyTo(tcpClientes, 0);

            // Percorre a lista de clientes TCP
            for (int i = 0; i < tcpClientes.Length; i++)
            {
                // Tenta enviar uma mensagem para cada cliente
                try
                {
                    // Se a msg estiver em branco ou a conexão for nula, sai...
                    if(Mensagem.Trim() == "" || tcpClientes[i] == null)
                    {
                        continue; // Pula para a próxima iteração do loop
                    }

                    // Envia a msg para o usuário atual no laço
                    swSenderSender = new StreamWriter(tcpClientes[i].GetStream());
                    swSenderSender.WriteLine("Administrador: " + Mensagem);
                    swSenderSender.Flush();
                    swSenderSender = null; // para que volte a ser nulo e não guarde info dos usuários
                }
                catch 
                {
                    // Se houver problema, o usuário não existe, então o remove
                    RemoveUsuario(tcpClientes[i]);
                }
            }
        }

        // Envia msgs de um usuário para todos os demais
        public static void EnviaMensagem(string Origem, string Mensagem)
        {
            StreamWriter swSenderSender;

            // Primeiro exibe a mensagem na aplicação
            e = new StatusChangedEventArgs(Origem + " disse: " + Mensagem);
            OnStatusChanged(e);

            // Cria um array de clientes TCPs com o tamanho do número de clientes existentes
            TcpClient[] tcpClientes = new TcpClient[Servidor.htUsuarios.Count];
            // Copia os objetos TcpCliente no array
            Servidor.htUsuarios.Values.CopyTo(tcpClientes, 0);
            // Percorre a lista de clientes TCP
            
            for(int i = 0;i < tcpClientes.Length;i++)
            {
                // Tenta enviar uma mensagem para cada cliente
                try
                {
                    // Se a msg estiver em branco ou a conexão for nula, sai...
                    if (Mensagem.Trim() == "" || tcpClientes[i] == null)
                    {
                        continue; // Pula para a próxima iteração do loop
                    }

                    // Envia a msg para o usuário atual no laço
                    swSenderSender = new StreamWriter(tcpClientes[i].GetStream());
                    swSenderSender.WriteLine(Origem + " disse: " + Mensagem);
                    swSenderSender.Flush();
                    swSenderSender = null; // para que volte a ser nulo e não guarde info dos usuários
                }
                catch
                {
                    // Se houver problema, o usuário não existe, então o remove
                    RemoveUsuario(tcpClientes[i]);
                }
            }
        }

        public void IniciaAtendimento()
        {
            try
            {
                // Pega IP
                IPAddress ipLocal = enderecoIP;
                int portaLocal = portaHost;

                // Cria um objeto TCP listener usando IP do servidor e porta definidas
                tlsCliente = new TcpListener(ipLocal, portaLocal);

                // Inicia o TCP listener e escuta as conexões
                tlsCliente.Start();

                // O laço while verifica se o servidor está rodando antes de checar as conexões
                ServRodando = true;

                // Inicia uma nova thread que hospeda o listener
                thrListener = new Thread(MantemAtendimento);
                thrListener.IsBackground = true; // isBackground encerra esse processo ao encerrar a aplicação
                thrListener.Start();
            }
            catch (Exception ex)
            {
            
            }
        }

        private void MantemAtendimento()
        {
            // Enquanto o servidor estiver rodando
            while (ServRodando)
            {
                // Aceita uma conexão pendente
                tcpCliente = tlsCliente.AcceptTcpClient();
                // Cria uma nova instância da conexão
                Conexao newConnection = new Conexao(tcpCliente);
            }
        }
    }
}
