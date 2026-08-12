import { useEffect, useState } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { api, ehDono, formatarReal } from '../servicos/api.js';

const OCULTO = 'R$ ••••,••';

/**
 * Cartões de faturamento com o "olhinho" para esconder valores
 * (mesma ideia do sistema antigo, agora com dados vindos da API).
 *
 * Aparece nas telas de mesa e balcão, que o atendente usa. Faturamento é do dono, então
 * o componente some inteiro para quem não é — e nem chega a chamar a API, que responderia 403.
 */
export default function ResumoFinanceiro() {
  const dono = ehDono();
  const [resumo, setResumo] = useState(null);
  const [visivel, setVisivel] = useState(false);
  const [erro, setErro] = useState('');

  useEffect(() => {
    if (!dono) return;
    api
      .get('/api/relatorios/resumo')
      .then(setResumo)
      .catch((excecao) => setErro(excecao.message));
  }, [dono]);

  if (!dono) return null;
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
