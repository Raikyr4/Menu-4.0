import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { addComandaItem, listMesas } from '../../api/comandas';

export function useTables() {
  return useQuery({
    queryKey: ['mesas'],
    queryFn: listMesas
  });
}

export function useAddItem() {
  const client = useQueryClient();

  return useMutation({
    mutationFn: addComandaItem,
    onSuccess: () => client.invalidateQueries({ queryKey: ['mesas'] })
  });
}
