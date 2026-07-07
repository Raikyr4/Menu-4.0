import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { legacyApi } from '../api/legacy';

export function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [nome, setNome] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  async function onLogin(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      const data = await legacyApi.login({ username, password });
      sessionStorage.setItem('nome', data.nome);
      navigate('/frente');
    } catch (err: any) {
      setError(err?.response?.data ?? 'Falha no login');
    }
  }

  async function onRegister(e: FormEvent) {
    e.preventDefault();
    setError('');
    try {
      await legacyApi.register({ nome, username, password });
      alert('Usuário criado com sucesso!');
    } catch (err: any) {
      setError(err?.response?.data ?? 'Falha no cadastro');
    }
  }

  return (
    <main>
      <h1>Login</h1>
      {error && <p>{error}</p>}
      <form onSubmit={onLogin} className="card">
        <input placeholder="username" value={username} onChange={(e) => setUsername(e.target.value)} />
        <input placeholder="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button type="submit">Entrar</button>
      </form>
      <h2>Cadastrar</h2>
      <form onSubmit={onRegister} className="card">
        <input placeholder="nome" value={nome} onChange={(e) => setNome(e.target.value)} />
        <input placeholder="username" value={username} onChange={(e) => setUsername(e.target.value)} />
        <input placeholder="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button type="submit">Cadastrar</button>
      </form>
    </main>
  );
}
