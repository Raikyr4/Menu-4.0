import { useEffect, useState } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { api, formatarReal } from '../servicos/api.js';

const OCULTO = 'R$ ••••,••';

/**
 * Cartões de faturamento com o "olhinho" para esconder valores
 * (mesma ideia do sistema antigo, agora com dados vindos da API).
 */
export default function ResumoFinanceiro() {
  const [resumo, setResumo] = useState(null);
  const [visivel, setVisivel] = useState(false);
  const [erro, setErro] = useState('');

  useEffect(() => {
    api
      .get('/api/relatorios/resumo')
      .then(setResumo)
      .catch((excecao) => setErro(excecao.message));
  }, []);

  if (erro) return <div className="alerta alerta-erro">{erro}</div>;

  const cartoes = [
    { rotulo: 'Faturamento de hoje', valor: resumo?.faturamentoHoje },
    { rotulo: 'Faturamento total', valor: resumo?.faturamentoTotal },
    { rotulo: 'Em aberto nas mesas', valor: resumo?.totalEmAbertoNasMesas },
  ];

  return (
    <div className="painel-resumo">
      {cartoes.map((cartao) => (
        <div className="cartao cartao-resumo" key={cartao.rotulo}>
          <div className="rotulo">
            {cartao.rotulo}
            <button
              className="botao-olho"
              title={visivel ? 'Esconder valores' : 'Mostrar valores'}
              onClick={() => setVisivel(!visivel)}
            >
              {visivel ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          <div className="valor">
            {visivel ? formatarReal(cartao.valor) : OCULTO}
          </div>
        </div>
      ))}
    </div>
  );
}
