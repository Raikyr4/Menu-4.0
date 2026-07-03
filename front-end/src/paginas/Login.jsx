import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UtensilsCrossed } from 'lucide-react';
import { api, salvarSessao } from '../servicos/api.js';

export default function Login() {
  const navegar = useNavigate();
  const [aba, setAba] = useState('entrar');
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');
  const [enviando, setEnviando] = useState(false);

  const [nomeUsuario, setNomeUsuario] = useState('');
  const [senha, setSenha] = useState('');
  const [nome, setNome] = useState('');

  function trocarAba(nova) {
    setAba(nova);
    setErro('');
    setSucesso('');
  }

  async function entrar(evento) {
    evento.preventDefault();
    setErro('');
    setEnviando(true);
    try {
      const resposta = await api.post('/api/autenticacao/login', { nomeUsuario, senha });
      salvarSessao(resposta.token, resposta.nome);
      navegar('/');
    } catch (excecao) {
      setErro(excecao.message);
    } finally {
      setEnviando(false);
    }
  }

  async function cadastrar(evento) {
    evento.preventDefault();
    setErro('');
    setSucesso('');
    setEnviando(true);
    try {
      await api.post('/api/autenticacao/cadastro', { nomeUsuario, senha, nome });
      trocarAba('entrar');
      setSucesso('Conta criada! Agora é só entrar.');
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
        <h1>Gestão do restaurante</h1>
        <p className="descricao">Mesas, balcão e comandas em um só lugar</p>

        <div className="abas">
          <button className={aba === 'entrar' ? 'ativa' : ''} onClick={() => trocarAba('entrar')}>
            Entrar
          </button>
          <button className={aba === 'cadastrar' ? 'ativa' : ''} onClick={() => trocarAba('cadastrar')}>
            Criar conta
          </button>
        </div>

        {erro && <div className="alerta alerta-erro">{erro}</div>}
        {sucesso && <div className="alerta alerta-sucesso">{sucesso}</div>}

        {aba === 'entrar' ? (
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
          </form>
        ) : (
          <form onSubmit={cadastrar}>
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
                placeholder="Mínimo 4 caracteres"
                minLength={4}
                required
              />
            </div>
            <button className="botao botao-primario botao-largo" disabled={enviando}>
              {enviando ? 'Criando...' : 'Criar conta'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
