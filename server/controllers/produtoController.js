const pool = require('../db');

exports.getProdutos = async (req, res) => {
  const result = await pool.query('SELECT * FROM tb_produto');
  res.status(200).json(result.rows);
};

