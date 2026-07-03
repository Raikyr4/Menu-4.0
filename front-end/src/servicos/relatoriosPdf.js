// Geração de relatórios financeiros em PDF (jsPDF + autotable)
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import { dataPuraLocal, formatarReal } from './api.js';

const COR_PRIMARIA = [232, 163, 61];
const COR_TEXTO = [40, 36, 28];

function dataLocal(dataIso) {
  return dataPuraLocal(dataIso).toLocaleDateString('pt-BR');
}

function iniciarDocumento(titulo, subtitulo) {
  const doc = new jsPDF();

  doc.setFillColor(...COR_PRIMARIA);
  doc.rect(0, 0, 210, 26, 'F');
  doc.setTextColor(36, 26, 5);
  doc.setFontSize(16);
  doc.setFont('helvetica', 'bold');
  doc.text('Menu 4.0 — Gestão do Restaurante', 14, 11);
  doc.setFontSize(11);
  doc.setFont('helvetica', 'normal');
  doc.text(titulo, 14, 19);

  doc.setTextColor(...COR_TEXTO);
  doc.setFontSize(9);
  doc.text(subtitulo, 14, 33);
  doc.text(`Gerado em ${new Date().toLocaleString('pt-BR')}`, 196, 33, { align: 'right' });

  return doc;
}

function tabela(doc, colunas, linhas, linhaRodape) {
  autoTable(doc, {
    startY: 38,
    head: [colunas],
    body: linhas,
    foot: linhaRodape ? [linhaRodape] : undefined,
    styles: { fontSize: 9, textColor: COR_TEXTO },
    headStyles: { fillColor: COR_PRIMARIA, textColor: [36, 26, 5], fontStyle: 'bold' },
    footStyles: { fillColor: [245, 238, 224], textColor: COR_TEXTO, fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [250, 247, 240] },
  });
}

export function pdfCaixaDiario(dadosCaixa, dias) {
  const doc = iniciarDocumento(
    'Relatório de caixa diário',
    `Período: últimos ${dias} dias (o caixa vira automaticamente à meia-noite)`
  );

  const linhas = dadosCaixa.map((dia) => [
    dataLocal(dia.data),
    String(dia.comandasFechadas),
    String(dia.quantidadePagamentos),
    formatarReal(dia.total),
  ]);

  const totalPeriodo = dadosCaixa.reduce((soma, dia) => soma + Number(dia.total), 0);

  tabela(
    doc,
    ['Data', 'Comandas fechadas', 'Pagamentos', 'Total recebido'],
    linhas,
    ['Total do período', '', '', formatarReal(totalPeriodo)]
  );

  doc.save(`caixa-diario-${dias}-dias.pdf`);
}

export function pdfProdutosMaisVendidos(produtos, dias) {
  const doc = iniciarDocumento(
    'Produtos mais vendidos',
    `Período: últimos ${dias} dias · comandas fechadas`
  );

  const linhas = produtos.map((produto, indice) => [
    String(indice + 1),
    produto.nome,
    produto.categoria,
    String(produto.quantidade),
    formatarReal(produto.total),
  ]);

  tabela(doc, ['#', 'Produto', 'Categoria', 'Qtde vendida', 'Total'], linhas);
  doc.save(`produtos-mais-vendidos-${dias}-dias.pdf`);
}

export function pdfFormasPagamento(formas, dias) {
  const doc = iniciarDocumento(
    'Vendas por forma de pagamento',
    `Período: últimos ${dias} dias`
  );

  const totalGeral = formas.reduce((soma, forma) => soma + Number(forma.total), 0);

  const linhas = formas.map((forma) => [
    forma.forma,
    String(forma.quantidade),
    formatarReal(forma.total),
    totalGeral > 0 ? `${((forma.total / totalGeral) * 100).toFixed(1)}%` : '0%',
  ]);

  tabela(
    doc,
    ['Forma', 'Qtde de pagamentos', 'Total', 'Participação'],
    linhas,
    ['Total', '', formatarReal(totalGeral), '100%']
  );

  doc.save(`formas-pagamento-${dias}-dias.pdf`);
}
