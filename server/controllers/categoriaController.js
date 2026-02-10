const pool = require('../db');

exports.getCategorias = async (req, res) => {
  const result = await pool.query('SELECT * FROM tb_categoria');
  res.status(200).json(result.rows);
};
