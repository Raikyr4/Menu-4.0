import { TableCard } from '../components/TableCard';
import { useAddItem, useTables } from '../features/tables/useTables';

export function TablesPage() {
  const { data, isLoading, isError, error } = useTables();
  const addItem = useAddItem();

  if (isLoading) return <p>Carregando mesas...</p>;
  if (isError) return <p>Erro ao carregar mesas: {(error as Error).message}</p>;

  return (
    <main>
      <h1>Mesas</h1>
      <div className="grid">
        {data?.map((mesa) => (
          <TableCard
            key={mesa.id}
            mesa={mesa}
            onAddItem={(mesaId) =>
              addItem.mutate({
                atendimentoId: mesaId,
                produtoId: 1,
                tipo: 'mesa',
                taxaHabilitada: true
              })
            }
          />
        ))}
      </div>
    </main>
  );
}
