import { useEffect } from 'react';
import { X } from 'lucide-react';

/** Modal padrão do sistema: fecha no X, clicando fora ou com Esc. */
export default function Modal({ titulo, aberto, aoFechar, children, largo = false }) {
  useEffect(() => {
    if (!aberto) return;
    const aoTeclar = (evento) => evento.key === 'Escape' && aoFechar();
    document.addEventListener('keydown', aoTeclar);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', aoTeclar);
      document.body.style.overflow = '';
    };
  }, [aberto, aoFechar]);

  if (!aberto) return null;

  return (
    <div className="sobreposicao-modal" onClick={aoFechar}>
      <div className={`cartao modal ${largo ? 'modal-largo' : ''}`} onClick={(evento) => evento.stopPropagation()}>
        <div className="modal-cabecalho">
          <h3>{titulo}</h3>
          <button className="botao-icone" title="Fechar" onClick={aoFechar}>
            <X size={18} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
