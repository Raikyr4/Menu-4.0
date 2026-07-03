/**
 * Campo de dinheiro com autoformatação estilo caixa registradora:
 * o usuário digita só números e o valor se monta da direita para a
 * esquerda ("1590" -> "15,90"), sempre com 2 casas decimais.
 *
 * O componente trabalha com número (em reais) no estado do pai:
 *   <CampoDinheiro valor={preco} aoMudar={setPreco} />
 */
export function formatarDinheiro(valor) {
  if (valor === null || valor === undefined || valor === '') return '';
  return Number(valor).toLocaleString('pt-BR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

export default function CampoDinheiro({ valor, aoMudar, id, placeholder = '0,00', ...resto }) {
  function aoDigitar(evento) {
    const digitos = evento.target.value.replace(/\D/g, '').slice(0, 12);
    aoMudar(digitos === '' ? null : Number(digitos) / 100);
  }

  return (
    <input
      id={id}
      inputMode="numeric"
      placeholder={placeholder}
      value={formatarDinheiro(valor)}
      onChange={aoDigitar}
      {...resto}
    />
  );
}
