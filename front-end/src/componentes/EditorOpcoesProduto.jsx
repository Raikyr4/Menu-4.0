import { Plus, Trash2 } from 'lucide-react';
import CampoDinheiro from './CampoDinheiro.jsx';

let proximaChave = 1;
const chave = () => `nova-${proximaChave++}`;

export function prepararGruposParaEdicao(grupos = []) {
  return grupos.map((grupo) => ({
    ...grupo,
    _chave: grupo.id ?? chave(),
    opcoes: (grupo.opcoes ?? []).map((opcao) => ({
      ...opcao,
      _chave: opcao.id ?? chave(),
      precoAdicional: Number(opcao.precoAdicional) || null,
    })),
  }));
}

export default function EditorOpcoesProduto({ grupos, aoMudar }) {
  function atualizarGrupo(indice, campo, valor) {
    aoMudar(grupos.map((grupo, i) => (i === indice ? { ...grupo, [campo]: valor } : grupo)));
  }

  function removerGrupo(indice) {
    aoMudar(grupos.filter((_, i) => i !== indice));
  }

  function adicionarGrupo() {
    aoMudar([
      ...grupos,
      {
        _chave: chave(),
        nome: '',
        obrigatorio: true,
        selecaoMultipla: false,
        opcoes: [{ _chave: chave(), nome: '', precoAdicional: null }],
      },
    ]);
  }

  function atualizarOpcao(indiceGrupo, indiceOpcao, campo, valor) {
    aoMudar(grupos.map((grupo, i) => {
      if (i !== indiceGrupo) return grupo;
      return {
        ...grupo,
        opcoes: grupo.opcoes.map((opcao, j) =>
          j === indiceOpcao ? { ...opcao, [campo]: valor } : opcao),
      };
    }));
  }

  function adicionarOpcao(indiceGrupo) {
    aoMudar(grupos.map((grupo, i) => i === indiceGrupo
      ? { ...grupo, opcoes: [...grupo.opcoes, { _chave: chave(), nome: '', precoAdicional: null }] }
      : grupo));
  }

  function removerOpcao(indiceGrupo, indiceOpcao) {
    aoMudar(grupos.map((grupo, i) => i === indiceGrupo
      ? { ...grupo, opcoes: grupo.opcoes.filter((_, j) => j !== indiceOpcao) }
      : grupo));
  }

  return (
    <section className="editor-opcoes">
      <div className="cabecalho-editor-opcoes">
        <div>
          <h4>Opções e adicionais</h4>
          <p>Crie grupos como Tamanho, Molho, Recheio ou Acompanhamento.</p>
        </div>
        <button className="botao botao-fantasma" type="button" onClick={adicionarGrupo}>
          <Plus size={15} /> Novo grupo
        </button>
      </div>

      {grupos.length === 0 && (
        <div className="vazio-opcoes">Produto simples, sem escolhas adicionais.</div>
      )}

      {grupos.map((grupo, indiceGrupo) => (
        <div className="grupo-opcoes-editor" key={grupo._chave}>
          <div className="linha-grupo-opcoes">
            <div className="campo">
              <label>Nome do grupo</label>
              <input
                value={grupo.nome}
                placeholder="Ex.: Tamanho"
                onChange={(e) => atualizarGrupo(indiceGrupo, 'nome', e.target.value)}
              />
            </div>
            <div className="campo">
              <label>Tipo de escolha</label>
              <select
                value={grupo.selecaoMultipla ? 'multipla' : 'unica'}
                onChange={(e) => atualizarGrupo(indiceGrupo, 'selecaoMultipla', e.target.value === 'multipla')}
              >
                <option value="unica">Escolha única</option>
                <option value="multipla">Múltiplas escolhas</option>
              </select>
            </div>
            <button
              className="botao-icone excluir"
              type="button"
              title="Remover grupo"
              onClick={() => removerGrupo(indiceGrupo)}
            >
              <Trash2 size={16} />
            </button>
          </div>

          <label className="opcao-taxa obrigatoriedade-grupo">
            <input
              type="checkbox"
              checked={grupo.obrigatorio}
              onChange={(e) => atualizarGrupo(indiceGrupo, 'obrigatorio', e.target.checked)}
            />
            Cliente precisa escolher neste grupo
          </label>

          <div className="lista-opcoes-editor">
            {grupo.opcoes.map((opcao, indiceOpcao) => (
              <div className="linha-opcao-editor" key={opcao._chave}>
                <input
                  value={opcao.nome}
                  placeholder="Nome da opção"
                  aria-label="Nome da opção"
                  onChange={(e) => atualizarOpcao(indiceGrupo, indiceOpcao, 'nome', e.target.value)}
                />
                <CampoDinheiro
                  valor={opcao.precoAdicional}
                  aoMudar={(valor) => atualizarOpcao(indiceGrupo, indiceOpcao, 'precoAdicional', valor)}
                  aria-label="Valor da opção"
                />
                <button
                  className="botao-icone excluir"
                  type="button"
                  title="Remover opção"
                  onClick={() => removerOpcao(indiceGrupo, indiceOpcao)}
                >
                  <Trash2 size={15} />
                </button>
              </div>
            ))}
          </div>
          <button className="botao-link" type="button" onClick={() => adicionarOpcao(indiceGrupo)}>
            <Plus size={14} /> Adicionar opção
          </button>
        </div>
      ))}
    </section>
  );
}
