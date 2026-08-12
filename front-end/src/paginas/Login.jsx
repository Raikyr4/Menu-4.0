import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UtensilsCrossed } from 'lucide-react';
import { api, salvarSessao } from '../servicos/api.js';

/**
 * Entrar no sistema. Não existe mais "criar conta" aqui: conta é criada pelo dono, dentro
 * do Administrativo. A única exceção é a instalação nova, que ainda não tem ninguém para
 * autorizar — nesse caso a própria tela oferece criar o primeiro dono.
 */
export default function Login() {
  const navegar = useNavigate();
  const [primeiroAcesso, setPrimeiroAcesso] = useState(null);
  const [erro, setErro] = useState('');
  const [enviando, setEnviando] = useState(false);

  const [nomeUsuario, setNomeUsuario] = useState('');
  const [senha, setSenha] = useState('');
  const [nome, setNome] = useState('');

  useEffect(() => {
    api
      .get('/api/autenticacao/primeiro-acesso')
      .then((resposta) => setPrimeiroAcesso(Boolean(resposta?.precisaDoPrimeiroUsuario)))
      .catch(() => setPrimeiroAcesso(false));
  }, []);

  async function entrar(evento) {
    evento.preventDefault();
    setErro('');
    setEnviando(true);
    try {
      const resposta = await api.post('/api/autenticacao/login', { nomeUsuario, senha });
      salvarSessao(resposta.token, resposta.nome, resposta.papel);
      navegar('/');
    } catch (excecao) {
      setErro(excecao.message);
    } finally {
      setEnviando(false);
    }
  }

  async function criarPrimeiroDono(evento) {
    evento.preventDefault();
    setErro('');
    setEnviando(true);
    try {
      await api.post('/api/autenticacao/cadastro', { nomeUsuario, senha, nome });
      const resposta = await api.post('/api/autenticacao/login', { nomeUsuario, senha });
      salvarSessao(resposta.token, resposta.nome, resposta.papel);
      navegar('/');
    } catch (excecao) {
      setErro(excecao.message);
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="pagina-login">
      <div className="cartao cartao-login">
        <div className="marca">
          <span className="logo"><UtensilsCrossed size={18} /></span>
          Menu 4.0
        </div>
        <h1>{primeiroAcesso ? 'Primeiro acesso' : 'Gestão do restaurante'}</h1>
        <p className="descricao">
          {primeiroAcesso
            ? 'Nenhuma conta existe ainda. Crie a conta do dono para começar.'
            : 'Mesas, balcão e comandas em um só lugar'}
        </p>

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {primeiroAcesso ? (
          <form onSubmit={criarPrimeiroDono}>
            <div className="campo">
              <label htmlFor="nome">Nome completo</label>
              <input
                id="nome"
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                placeholder="Como aparece no sistema"
                required
                autoFocus
              />
            </div>
            <div className="campo">
              <label htmlFor="usuario-novo">Usuário</label>
              <input
                id="usuario-novo"
                value={nomeUsuario}
                onChange={(e) => setNomeUsuario(e.target.value)}
                placeholder="usuario.de.acesso"
                minLength={3}
                required
              />
            </div>
            <div className="campo">
              <label htmlFor="senha-nova">Senha</label>
              <input
                id="senha-nova"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                placeholder="Ao menos 8 caracteres, com letras e números"
                minLength={8}
                required
              />
            </div>
            <button className="botao botao-primario botao-largo" disabled={enviando}>
              {enviando ? 'Criando...' : 'Criar conta do dono'}
            </button>
          </form>
        ) : (
          <form onSubmit={entrar}>
            <div className="campo">
              <label htmlFor="usuario">Usuário</label>
              <input
                id="usuario"
                value={nomeUsuario}
                onChange={(e) => setNomeUsuario(e.target.value)}
                placeholder="seu.usuario"
                required
                autoFocus
              />
            </div>
            <div className="campo">
              <label htmlFor="senha">Senha</label>
              <input
                id="senha"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                placeholder="••••••••"
                required
              />
            </div>
            <button className="botao botao-primario botao-largo" disabled={enviando}>
              {enviando ? 'Entrando...' : 'Entrar'}
            </button>
            <p className="descricao descricao-rodape">
              Sem acesso? Peça uma conta ao dono do restaurante.
            </p>
          </form>
        )}
      </div>
    </div>
  );
}
