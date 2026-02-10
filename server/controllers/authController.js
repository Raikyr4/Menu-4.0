const pool = require('../db');
const bcrypt = require('bcrypt');

exports.login = async (req, res) => {
  // lógica de login
  const { username, password } = req.body;

  
  // Buscar usuário no banco de dados
  const user = await pool.query('SELECT * FROM tb_users WHERE username = $1', [username]);

  if (user.rows.length > 0) {

    const userFirst = user.rows[0];

      // Comparar senha
      const validPassword = await bcrypt.compare(password, userFirst.password);

      if (!validPassword) {
          return res.status(400).json('Senha incorreta');
      }

      res.status(200).json({ message: 'Login bem sucedido', nome: user.rows[0].nome });
  } else {
      res.status(400).json('Usuário ou Senha incorreta');
  }
};

exports.register = async (req, res) => {
  // lógica de registro
  
  const { username, password, nome} = req.body;

  // Verificar se o usuário já existe
  const user = await pool.query('SELECT * FROM tb_users WHERE username = $1', [username]);

  if (user.rows.length > 0) {
      return res.status(400).json('Usuário já existe');
  }

  // Criptografar a senha
  const salt = await bcrypt.genSalt(10);
  const hashedPassword = await bcrypt.hash(password, salt);

  // Inserir o novo usuário no banco de dados
  await pool.query('INSERT INTO tb_users (username, password, nome) VALUES ($1, $2, $3)', [username, hashedPassword, nome]);

  res.status(200).json('Usuário registrado com sucesso');
};
