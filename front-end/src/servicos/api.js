// Cliente HTTP central: injeta o token JWT e trata sessão expirada.

const CHAVE_TOKEN = 'menu.token';
const CHAVE_NOME = 'menu.nome';

export function salvarSessao(token, nome) {
  localStorage.setItem(CHAVE_TOKEN, token);
  localStorage.setItem(CHAVE_NOME, nome);
}

export function limparSessao() {
  localStorage.removeItem(CHAVE_TOKEN);
  localStorage.removeItem(CHAVE_NOME);
}

export function obterToken() {
  return localStorage.getItem(CHAVE_TOKEN);
}

export function obterNome() {
  return localStorage.getItem(CHAVE_NOME) || '';
}

export function estaLogado() {
  return Boolean(obterToken());
}

async function requisitar(caminho, opcoes = {}) {
  const cabecalhos = { 'Content-Type': 'application/json', ...opcoes.headers };
  const token = obterToken();
  if (token) cabecalhos.Authorization = `Bearer ${token}`;

  const resposta = await fetch(caminho, { ...opcoes, headers: cabecalhos });

  if (resposta.status === 401) {
    // Sessão expirada ou token inválido: volta para o login
    limparSessao();
    window.location.href = '/login';
    throw new Error('Sessão expirada. Entre novamente.');
  }

  const corpo = resposta.status === 204 ? null : await resposta.json().catch(() => null);

  if (!resposta.ok) {
    const mensagem =
      corpo?.mensagem ||
      (corpo?.errors && Object.values(corpo.errors).flat().join(' ')) ||
      'Ocorreu um erro inesperado. Tente novamente.';
    throw new Error(mensagem);
  }

  return corpo;
}

export const api = {
  get: (caminho) => requisitar(caminho),
  post: (caminho, dados) =>
    requisitar(caminho, { method: 'POST', body: dados ? JSON.stringify(dados) : undefined }),
  put: (caminho, dados) =>
    requisitar(caminho, { method: 'PUT', body: JSON.stringify(dados) }),
  delete: (caminho) => requisitar(caminho, { method: 'DELETE' }),
};

/**
 * Converte data pura da API ("2026-07-02") em Date local, sem deslocamento
 * de fuso (new Date('2026-07-02') interpretaria como UTC e voltaria um dia).
 */
export function dataPuraLocal(dataIso) {
  const [ano, mes, dia] = String(dataIso).slice(0, 10).split('-').map(Number);
  return new Date(ano, mes - 1, dia);
}

export function formatarReal(valor) {
  return Number(valor ?? 0).toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  });
}
