$(function () {
    $('.nome').text(sessionStorage.getItem('nome'));
    $('.myButtons').css('width', '90%');
    Redirecionar();
    VerTotal();
    VerTotalMesa();
    MesaOcupada();
});



function Redirecionar() {
    let mybutton = $('.botoes');
    mybutton.click(function () {
        window.location.href = '../comanda/comanda.html?mesa_id=' + $(this).data("mesa_id");
    });

    let back = $('.back');
    back.click(function () {
        window.location.href = '../frente/frente.html';
    });
}


function CalculaMontante(seeType, type) {
    $.ajax({
        url: '/' + seeType,
        method: 'GET',
        success: function (response) {

            var somaTotal = response.somaTotal;

            // Formatar como moeda
            var somaTotalFormatada = 'R$ ' + parseFloat(somaTotal).toLocaleString('pt-BR', { minimumFractionDigits: 2 });

            $("." + type).text(somaTotalFormatada);
        },
        error: function (xhr, status, error) {
            // Mostrar erro
            console.log('Erro ao buscar a soma total: ' + xhr.responseText);
        }
    });


}

function VerTotal() {
    let eye = $(".verTotal");
    eye.click(function () {
        if ($(this).text() == "visibility_off") {

            $(this).text("visibility");
            CalculaMontante('verTotal', 'montante');
        }
        else {
            $(this).text("visibility_off");
            $(".montante").text("R$ ****,**");
        }
    });
}

function VerTotalMesa() {
    let eye = $(".verMesa");
    eye.click(function () {
        if ($(this).text() == "visibility_off") {

            $(this).text("visibility");
            CalculaMontante('verMesa', 'totalNaMesa');
        }
        else {
            $(this).text("visibility_off");
            $(".totalNaMesa").text("R$ ****,**");
        }
    });
}

function MesaOcupada()
{
    $.ajax({
        type: 'GET',
        url: '/getMesasOcupadas',
        contentType: 'application/json',
        success: function (response) {
           console.log(response);
        
        let objetosComProdutos = response.filter(objeto => objeto.produtos !== "");
        
        objetosComProdutos.forEach(objeto => {
            let id = objeto.id;
            $(`a[data-mesa_id=${id}]`).removeClass('botoes').addClass('botoesOcupado');
        });
        
        },
        error: function (xhr) {
            alert('erro ao consultar status das mesas');
        }
    });
}