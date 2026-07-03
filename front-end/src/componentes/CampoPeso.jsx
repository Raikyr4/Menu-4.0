export function formatarPesoKg(valor) {
  if (valor === null || valor === undefined || valor === '') return '';
  return Number(valor).toLocaleString('pt-BR', {
    minimumFractionDigits: 3,
    maximumFractionDigits: 3,
  });
}

export default function CampoPeso({ valor, aoMudar, id, ...resto }) {
  function aoDigitar(evento) {
    const digitos = evento.target.value.replace(/\D/g, '').slice(0, 6);
    aoMudar(digitos === '' ? null : Number(digitos) / 1000);
  }

  const gramas = Math.round(Number(valor || 0) * 1000);

  return (
    <div className="campo-peso">
      <div className="entrada-com-sufixo">
        <input
          id={id}
          inputMode="numeric"
          placeholder="0,000"
          value={formatarPesoKg(valor)}
          onChange={aoDigitar}
          {...resto}
        />
        <span>kg</span>
      </div>
      <small>{valor ? `${gramas.toLocaleString('pt-BR')} g` : 'Digite o peso em gramas'}</small>
    </div>
  );
}
