import { useEffect, useMemo, useState } from 'react';
import CampoPeso from './CampoPeso.jsx';
import Modal from './Modal.jsx';
import { formatarReal } from '../servicos/api.js';

export default function ConfigurarProdutoModal({ produto, aoFechar, aoConfirmar }) {
  const [selecionadas, setSelecionadas] = useState({});
  const [pesoKg, setPesoKg] = useState(null);
  const [erro, setErro] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    setSelecionadas({});
    setPesoKg(null);
    setErro('');
    setSalvando(false);
  }, [produto?.id]);

  const opcaoPorId = useMemo(() => new Map(
    (produto?.gruposOpcoes ?? []).flatMap((grupo) =>
      grupo.opcoes.map((opcao) => [opcao.id, opcao]))
  ), [produto]);

  const idsSelecionados = Object.values(selecionadas).flat();
  const valorOpcoes = idsSelecionados.reduce(
    (total, id) => total + Number(opcaoPorId.get(id)?.precoAdicional ?? 0), 0);
  const vendidoPorPeso = Number(produto?.valorKg) > 0;
  const total = vendidoPorPeso
    ? Number(pesoKg ?? 0) * Number(produto?.valorKg ?? 0) + valorOpcoes
    : Number(produto?.preco ?? 0) + valorOpcoes;

  function escolher(grupo, opcaoId) {
    setErro('');
    setSelecionadas((atual) => {
      const ids = atual[grupo.id] ?? [];
      if (!grupo.selecaoMultipla) return { ...atual, [grupo.id]: [opcaoId] };
      return {
        ...atual,
        [grupo.id]: ids.includes(opcaoId) ? ids.filter((id) => id !== opcaoId) : [...ids, opcaoId],
      };
    });
  }

  async function confirmar(evento) {
    evento.preventDefault();
    for (const grupo of produto.gruposOpcoes ?? []) {
      if (grupo.obrigatorio && !(selecionadas[grupo.id]?.length)) {
        setErro(`Escolha uma opção em “${grupo.nome}”.`);
        return;
      }
    }
    if (vendidoPorPeso && (!pesoKg || pesoKg < 0.001)) {
      setErro('Informe o peso do produto.');
      return;
    }

    setSalvando(true);
    setErro('');
    try {
      await aoConfirmar({ quantidade: vendidoPorPeso ? pesoKg : 1, opcaoIds: idsSelecionados });
    } catch (excecao) {
      setErro(excecao.message);
      setSalvando(false);
    }
  }

  return (
    <Modal titulo={produto?.nome ?? ''} aberto={Boolean(produto)} aoFechar={aoFechar} largo>
      {produto && (
        <form onSubmit={confirmar}>
          {erro && <div className="alerta alerta-erro">{erro}</div>}

          {vendidoPorPeso && (
            <div className="bloco-configuracao-produto">
              <div className="titulo-configuracao">
                <div>
                  <h4>Informe o peso</h4>
                  <p>{formatarReal(produto.valorKg)} por kg</p>
                </div>
              </div>
              <div className="campo">
                <label htmlFor="peso-produto">Peso</label>
                <CampoPeso id="peso-produto" valor={pesoKg} aoMudar={setPesoKg} autoFocus />
              </div>
            </div>
          )}

          {(produto.gruposOpcoes ?? []).map((grupo) => (
            <fieldset className="bloco-configuracao-produto" key={grupo.id}>
              <legend className="titulo-configuracao">
                <span>{grupo.nome}</span>
                <small>{grupo.obrigatorio ? 'Obrigatório' : 'Opcional'} · {grupo.selecaoMultipla ? 'Escolha quantas quiser' : 'Escolha uma'}</small>
              </legend>
              <div className="lista-escolhas-produto">
                {grupo.opcoes.map((opcao) => {
                  const marcada = (selecionadas[grupo.id] ?? []).includes(opcao.id);
                  return (
                    <label className={`escolha-produto ${marcada ? 'marcada' : ''}`} key={opcao.id}>
                      <input
                        type={grupo.selecaoMultipla ? 'checkbox' : 'radio'}
                        name={`grupo-${grupo.id}`}
                        checked={marcada}
                        onChange={() => escolher(grupo, opcao.id)}
                      />
                      <span>{opcao.nome}</span>
                      <strong>
                        {Number(opcao.precoAdicional) > 0
                          ? `${Number(produto.preco) > 0 ? '+' : ''}${formatarReal(opcao.precoAdicional)}`
                          : 'Incluso'}
                      </strong>
                    </label>
                  );
                })}
              </div>
            </fieldset>
          ))}

          <div className="resumo-configuracao-produto">
            <span>Valor deste item</span>
            <strong>{formatarReal(total)}</strong>
          </div>
          <div className="acoes-form">
            <button className="botao botao-primario" type="submit" disabled={salvando}>
              {salvando ? 'Adicionando...' : 'Adicionar à comanda'}
            </button>
            <button className="botao botao-fantasma" type="button" onClick={aoFechar}>Cancelar</button>
          </div>
        </form>
      )}
    </Modal>
  );
}
