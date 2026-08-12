import { useEffect, useState } from 'react';
import { UserPlus } from 'lucide-react';
import { api } from '../servicos/api.js';

const ROTULO_PAPEL = { DONO: 'Dono', OPERADOR: 'Atendente' };

const FORMULARIO_VAZIO = { nome: '', nomeUsuario: '', senha: '', papel: 'OPERADOR' };

/**
 * Gestão das contas de acesso. Saiu da tela de login (onde qualquer um criava conta) e
 * virou parte do Administrativo, que já é área de dono.
 *
 * Não há exclusão de conta: usuário é autor de lançamento, e apagar quem registrou uma
 * sangria destruiria a trilha. Quando for preciso tirar alguém do sistema, o caminho é
 * desativar — e isso ainda não existe.
 */
export default function PainelUsuarios() {
  const [usuarios, setUsuarios] = useState(null);
  const [formulario, setFormulario] = useState(FORMULARIO_VAZIO);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    carregar();
  }, []);

  async function carregar() {
    try {
      setUsuarios(await api.get('/api/autenticacao/usuarios'));
    } catch (excecao) {
      setErro(excecao.message);
    }
  }

  function alterar(campo, valor) {
    setFormulario((atual) => ({ ...atual, [campo]: valor }));
  }

  async function criar(evento) {
    evento.preventDefault();
    setErro('');
    setSucesso('');
    setEnviando(true);
    try {
      const criado = await api.post('/api/autenticacao/cadastro', formulario);
      setFormulario(FORMULARIO_VAZIO);
      setSucesso(`Conta de ${criado.nome} criada como ${ROTULO_PAPEL[criado.papel]}.`);
      await carregar();
    } catch (excecao) {
      setErro(excecao.message);
    } finally {
      setEnviando(false);
    }
  }

  return (
    <section className="cartao painel-cardapio" style={{ marginBottom: 22 }}>
      <h2 className="titulo-painel">Contas de acesso</h2>
      <p className="subtitulo-painel">
        Atendente lança pedido e fecha comanda. Dono também vê faturamento e altera o cardápio.
      </p>

      {erro && <div className="alerta alerta-erro">{erro}</div>}
      {sucesso && <div className="alerta alerta-sucesso">{sucesso}</div>}

      <form className="formulario-usuario" onSubmit={criar}>
        <div className="campo">
          <label htmlFor="usuario-nome">Nome completo</label>
          <input
            id="usuario-nome"
            value={formulario.nome}
            onChange={(e) => alterar('nome', e.target.value)}
            placeholder="Como aparece no sistema"
            required
          />
        </div>
        <div className="campo">
          <label htmlFor="usuario-login">Usuário</label>
          <input
            id="usuario-login"
            value={formulario.nomeUsuario}
            onChange={(e) => alterar('nomeUsuario', e.target.value)}
            placeholder="usuario.de.acesso"
            minLength={3}
            required
          />
        </div>
        <div className="campo">
          <label htmlFor="usuario-senha">Senha</label>
          <input
            id="usuario-senha"
            type="password"
            value={formulario.senha}
            onChange={(e) => alterar('senha', e.target.value)}
            placeholder="Letras e números, 8+"
            minLength={8}
            required
          />
        </div>
        <div className="campo">
          <label htmlFor="usuario-papel">Papel</label>
          <select
            id="usuario-papel"
            value={formulario.papel}
            onChange={(e) => alterar('papel', e.target.value)}
          >
            <option value="OPERADOR">Atendente</option>
            <option value="DONO">Dono</option>
          </select>
        </div>
        <button className="botao botao-primario" disabled={enviando}>
          <UserPlus size={16} /> {enviando ? 'Criando...' : 'Criar conta'}
        </button>
      </form>

      <div className="rolagem-x">
        <table className="tabela">
          <thead>
            <tr>
              <th>Nome</th>
              <th>Usuário</th>
              <th>Papel</th>
            </tr>
          </thead>
          <tbody>
            {(usuarios ?? []).map((usuario) => (
              <tr key={usuario.id}>
                <td>{usuario.nome}</td>
                <td>{usuario.nomeUsuario}</td>
                <td>{ROTULO_PAPEL[usuario.papel] ?? usuario.papel}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
