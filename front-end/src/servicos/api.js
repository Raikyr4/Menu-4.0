// Cliente HTTP central: injeta o token JWT e trata sessão expirada.

const CHAVE_TOKEN = 'menu.token';
const CHAVE_NOME = 'menu.nome';
const CHAVE_PAPEL = 'menu.papel';

export const PAPEL_DONO = 'DONO';

export function salvarSessao(token, nome, papel) {
  localStorage.setItem(CHAVE_TOKEN, token);
  localStorage.setItem(CHAVE_NOME, nome);
  localStorage.setItem(CHAVE_PAPEL, papel || '');
}

export function limparSessao() {
  localStorage.removeItem(CHAVE_TOKEN);
  localStorage.removeItem(CHAVE_NOME);
  localStorage.removeItem(CHAVE_PAPEL);
}

export function obterToken() {
  return localStorage.getItem(CHAVE_TOKEN);
}

export function obterNome() {
  return localStorage.getItem(CHAVE_NOME) || '';
}

/**
 * Papel guardado no login. Serve só para esconder o que o usuário não pode usar —
 * quem barra de verdade é o [Authorize(Roles = ...)] do servidor, e o papel que vale
 * é o que está assinado dentro do token.
 */
export function obterPapel() {
  return localStorage.getItem(CHAVE_PAPEL) || '';
}

export function ehDono() {
  return obterPapel() === PAPEL_DONO;
}

/** Decodifica o corpo do JWT sem validar assinatura — só para ler a expiração. */
function corpoDoToken(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const bytes = atob(base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '='));
    const texto = decodeURIComponent(
      bytes.replace(/./g, (c) => `%${c.charCodeAt(0).toString(16).padStart(2, '0')}`)
    );
    return JSON.parse(texto);
  } catch {
    return null;
  }
}

/**
 * Token presente não é token válido: expirado, toda chamada voltava 401 e o usuário
 * via a tela piscar antes de cair no login. Aqui a expiração é conferida antes.
 */
export function estaLogado() {
  const token = obterToken();
  if (!token) return false;

  const corpo = corpoDoToken(token);
  if (!corpo?.exp) return false;

  if (corpo.exp * 1000 <= Date.now()) {
    limparSessao();
    return false;
  }
  return true;
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

  if (resposta.status === 403) {
    throw new Error('Esta parte do sistema é do dono do restaurante.');
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
