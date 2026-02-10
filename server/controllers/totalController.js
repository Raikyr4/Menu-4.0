const pool = require('../db');

exports.verTotal = async (req, res) => {
  // lógica de verTotal
  
  const result = await pool.query('SELECT SUM(total) FROM tb_registro_diario');

  if (result.rows.length > 0) {
      const somaTotal = result.rows[0].sum;

      // Enviar a soma como resposta
      res.status(200).json({ somaTotal: somaTotal });
  } else {
      res.status(400).json('Nenhum registro encontrado');
  }
};

exports.verMesa = async (req, res) => {
  // lógica de verMesa
  const result = await pool.query('SELECT SUM(total) FROM tb_mesa');

  if (result.rows.length > 0) {
      const somaTotal = result.rows[0].sum;

      // Enviar a soma como resposta
      res.status(200).json({ somaTotal: somaTotal });
  } else {
      res.status(400).json('Nenhum registro encontrado');
  }
};
