export default function Carregando({ mensagem = 'Carregando...' }) {
  return (
    <div className="carregando">
      <div>
        <div className="spin" />
        {mensagem}
      </div>
    </div>
  );
}
