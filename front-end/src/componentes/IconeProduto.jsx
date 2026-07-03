import {
  Beef,
  Beer,
  CakeSlice,
  ChefHat,
  Cherry,
  Citrus,
  CookingPot,
  Croissant,
  CupSoda,
  Drumstick,
  Flame,
  GlassWater,
  Leaf,
  Milk,
  Package,
  Salad,
  Sandwich,
  Soup,
  Utensils,
  Wheat,
} from 'lucide-react';

// O nome do produto tem prioridade sobre a categoria: itens diferentes dentro
// da mesma seção passam a ter representações próprias e reconhecíveis.
const REGRAS_PRODUTO = [
  { palavras: ['cerveja', 'almaza'], Icone: Beer, cor: '#e8a33d' },
  { palavras: ['coca-cola', 'guarana'], Icone: CupSoda, cor: '#64a8dd' },
  { palavras: ['limao'], Icone: Citrus, cor: '#d6cf55' },
  { palavras: ['hortela', 'cebola', 'folha de uva', 'folha de repolho'], Icone: Leaf, cor: '#6fbf73' },
  { palavras: ['tomate cereja'], Icone: Cherry, cor: '#e2635a' },
  { palavras: ['frango', 'file de peito', 'michui'], Icone: Drumstick, cor: '#dc9257' },
  { palavras: ['shawarma', 'kebab', 'sanduiche'], Icone: Sandwich, cor: '#d8a24a' },
  { palavras: ['esfiha', 'esfirra', 'pao sirio'], Icone: Croissant, cor: '#d8a24a' },
  { palavras: ['kibe', 'quibe', 'kafta', 'carne fechada'], Icone: Beef, cor: '#d1705f' },
  { palavras: ['queijo', 'mussarela', 'mucarela', 'coalhada', 'cream cheese'], Icone: Milk, cor: '#f0d264' },
  { palavras: ['tabule', 'salada'], Icone: Salad, cor: '#6fbf73' },
  { palavras: ['arroz', 'mjadra', 'chairie', 'aletria'], Icone: CookingPot, cor: '#c09363' },
  { palavras: ['homus', 'babagan', 'caponata', 'ariche', 'muhamara', 'pate'], Icone: Soup, cor: '#e0925f' },
  { palavras: ['molho', 'caldo'], Icone: Flame, cor: '#e2635a' },
  { palavras: ['baklava', 'ninho castanha', 'doce'], Icone: CakeSlice, cor: '#e58fb1' },
  { palavras: ['combo', 'blend de sabores', 'petisqueira'], Icone: Package, cor: '#64a8dd' },
  { palavras: ['menu executivo'], Icone: ChefHat, cor: '#f3efe6' },
];

const REGRAS_CATEGORIA = [
  { palavras: ['bebidas'], Icone: GlassWater, cor: '#64a8dd' },
  { palavras: ['doces'], Icone: CakeSlice, cor: '#e58fb1' },
  { palavras: ['adicionais'], Icone: Wheat, cor: '#d8a24a' },
  { palavras: ['combos'], Icone: Package, cor: '#64a8dd' },
  { palavras: ['menu executivo'], Icone: ChefHat, cor: '#f3efe6' },
  { palavras: ['antepastos'], Icone: Soup, cor: '#e0925f' },
];

const PADRAO = { Icone: Utensils, cor: '#b3ab99' };

function normalizar(texto) {
  return (texto || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();
}

function encontrarRegra(texto, regras) {
  return regras.find((regra) => regra.palavras.some((palavra) => texto.includes(palavra)));
}

export function representacaoDoProduto(nomeProduto, nomeCategoria) {
  return encontrarRegra(normalizar(nomeProduto), REGRAS_PRODUTO)
    ?? encontrarRegra(normalizar(nomeCategoria), REGRAS_CATEGORIA)
    ?? PADRAO;
}

export default function IconeProduto({ nomeProduto, nomeCategoria, tamanho = 22 }) {
  const { Icone, cor } = representacaoDoProduto(nomeProduto, nomeCategoria);
  return (
    <span className="icone-produto" style={{ color: cor, background: `${cor}1f` }}>
      <Icone size={tamanho} strokeWidth={1.8} />
    </span>
  );
}
