$(function () {

    $('.nome').text(sessionStorage.getItem('nome'));
    
    IdentificaAtendimento();

     var checkbox = $('.checkboxTaxa .troca');
     checkbox.prop('checked', localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'true');

    if($('.tipo').val() == "balcao_id")
    {
        localStorage.setItem('checkbox_' +  $('.id_tipo').val(), 'true' );
        $('.v_tx_servico, .v_total_tx, .checkboxTaxa').hide();
    }

    

    Redirecionar();
    CarregaCategorias();
    CarregaProdutos();
    Pesquisar();
    VerCategoria();
    VerProduto();
    AletraQuantidade();
    EnviarProduto();
    EnviarCarrinho();
    CarregaComanda();
    ExcluiProduto();
    PagarParcial();
    ExcluiPagamento();
    TaxaDeServico();
    FecharComanda();

});




/**
 *  EXPLICAÇÃO:
 * 
 *   A função IdentificaAtendimento identifica o tipo de atendimento (mesa ou balcão) com base nos parâmetros da URL.
 * 
 * */
function IdentificaAtendimento() {
   let IurlParams = new URLSearchParams(window.location.search);
   let IparamsObj = {};

    for (var pair of IurlParams.entries()) {
        IparamsObj[pair[0]] = pair[1];
    }
    if ('mesa_id' in IparamsObj) 
    {
        $('.atendimento_tipo').text('MESA ' + Number(IparamsObj['mesa_id']))
        $('.tipo').val("mesa_id");
        $('.id_tipo').val(Number(IparamsObj['mesa_id']));
    } 
    else if ('balcao_id' in IparamsObj)
    {
        $('.atendimento_tipo').text('BALCÃO ' + Number(IparamsObj['balcao_id']))
        $('.tipo').val("balcao_id");
        $('.id_tipo').val(Number(IparamsObj['balcao_id']));
    }
}





/**
 *  EXPLICAÇÃO:
 * 
 *   A função Redirecionar adiciona um evento de clique ao elemento com a classe 'back'.
 *   Quando esse elemento é clicado, o usuário é redirecionado para a página 'mesa.html' ou 'balcao.html'
 *   dependendo dos parâmetros da URL.
 * 
 * */
function Redirecionar() {
    let Rback = $('.back');
    Rback.click(function () {

       let RurlParams = new URLSearchParams(window.location.search);
       let RparamsObj = {};

        for (var pair of RurlParams.entries()) {
            RparamsObj[pair[0]] = pair[1];
        }

        if ('mesa_id' in RparamsObj) 
        {
            window.location.href = '../mesa/mesa.html';
        }
        else if ('balcao_id' in RparamsObj) 
        {
            window.location.href = '../balcao/balcao.html';
        }
    });
}






/**
 *  EXPLICAÇÃO:
 * 
 *   A função CarregaCategorias faz uma requisição GET para o endpoint '/categorias' e, para cada categoria retornada,
 *   cria um novo elemento div com a classe 'itens', adiciona um elemento p com o nome da categoria e adiciona o div ao
 *   elemento com a classe 'categorias .content'.
 * 
 * */
function CarregaCategorias() {
    $.get('/categorias', function (data) {
        $.each(data, function (i, categoria) {
           let CitemDiv = $('<div>').addClass('itens');
           let CnomeCategoria = $('<p>').addClass('produto_nm').text(categoria.categoria_nm.toUpperCase());
           CitemDiv.append(CnomeCategoria);
           CitemDiv.attr('data-id', categoria.id);
           $('.categorias .content').append(CitemDiv);
        });
    });
}







/**
 *  EXPLICAÇÃO:
 * 
 *   A função CarregaProdutos faz uma requisição GET para o endpoint '/produtos' e, para cada produto retornado,
 *   cria um novo elemento div com a classe 'itens', adiciona um elemento p com o nome do produto e o preço,
 *   e adiciona o div ao elemento com a classe 'produtos .content'. Além disso, adiciona atributos de dados ao div
 *   para armazenar informações adicionais sobre o produto.
 * 
 * */
function CarregaProdutos() {
    $.get('/produtos', function (data) {
        $.each(data, function (i, produto) {
           let CPitemDiv = $('<div>').addClass('itens');
           let CPnomeProduto = $('<p>').addClass('produto_nm').text(produto.produto_nm.toUpperCase());
           let CPprecoProduto = $('<p>').addClass('preco').text('R$ ' + produto.preco);

            CPitemDiv.append(CPnomeProduto);
            CPitemDiv.append(CPprecoProduto);
            CPitemDiv.attr('data-produto_id', produto.id);
            CPitemDiv.attr('data-categoria_id', produto.categoria_id);

            if (produto.especial > 0) CPitemDiv.attr('data-especial', produto.especial);

            if (produto.valor_kg > 0) CPitemDiv.attr('data-kg', produto.valor_kg);

            $('.produtos .content').append(CPitemDiv);
        });
    });
}






/**
 *  EXPLICAÇÃO:
 * 
 *   A função Pesquisar adiciona eventos de foco e keyup ao elemento com o id 'search-bar'.
 *   Quando o elemento ganha foco, todos os itens de produto são exibidos e a descrição do produto é ocultada.
 *   Quando uma tecla é pressionada, a função filtra os itens de produto com base no valor do campo de pesquisa.
 * 
 * */
function Pesquisar() {
    $('#search-bar').on('focus', function () {
        $('.produtos .itens').show();
        $('.descricao_produto').css('display', 'none');
    });

    $('#search-bar').on('keyup', function () {
       let Pvalue = $(this).val().toLowerCase();

        $('.produtos .itens').filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(Pvalue) > -1)
        });
    });
}







/**
 *  EXPLICAÇÃO:
 * 
 *   A função VerCategoria adiciona um evento de clique aos elementos com a classe 'itens' dentro do elemento com a classe 'categorias'.
 *   Quando um desses elementos é clicado, a descrição do produto é ocultada e apenas os itens de produto que correspondem à categoria clicada são exibidos.
 * 
 * */
function VerCategoria() {
    $('.categorias').on('click', '.itens', function () {
        $('.descricao_produto').css('display', 'none');

       let VcategoriaId = $(this).data('id');

        $('.produtos .itens').hide();

        $('.produtos .itens[data-categoria_id="' + VcategoriaId + '"]').show();
    });
}





/**
 *  EXPLICAÇÃO:
 * 
 *   A função VerProduto adiciona um evento de clique aos elementos com a classe 'itens' dentro do elemento com a classe 'produtos'.
 *   Quando um desses elementos é clicado, verifica se o produto é especial, se tem um valor por kg ou se pertence a uma categoria.
 *   Dependendo dessas condições, oculta todos os produtos e mostra uma div específica (especial, kg ou descrição do produto).
 * 
 * */
function VerProduto() {
    $('.produtos').on('click', '.itens', function () {
       let VPprodutoEspecial = $(this).data('especial');
       let VPprodutoKg = $(this).data('kg');
       let VPcategoriaId = $(this).data('categoria_id');
       let VPpreco = $(this).find('.preco').text();
       let VPnomeProduto = $(this).find('.produto_nm').text();
       let VPproduto_id = $(this).data('produto_id');

        if (VPprodutoKg) 
        {
            $('.produtos .itens').hide();
        } 
        else if (VPcategoriaId) 
        {
            $('.produtos .itens').hide();
            $('.descricao_produto').css('display', 'flex');
            $('.descricao_produto .total_preco').text(VPpreco);
            $('.quantia').val(1);
            $('.descricao_produto').data('produto_id', VPproduto_id);
            $('.total_preco').data('preco', parseFloat(VPpreco.replace('R$', '').replace(',', '.'))); 
            $('.descricao_produto .produto_nome').text(VPnomeProduto); 
        }
    });
}






/**
 *  EXPLICAÇÃO:
 * 
 *   A função AletraQuantidade adiciona eventos de clique e entrada aos elementos com as classes 'qtd' e 'quantia', respectivamente.
 *   Quando um elemento 'qtd' é clicado, a quantidade é incrementada ou decrementada e o preço total é atualizado.
 *   Quando o valor de um elemento 'quantia' é alterado, o preço total é atualizado de acordo com a nova quantidade.
 * 
 * */
function AletraQuantidade() {
    $('.qtd').on('click', function () {
       let AQquantia = parseInt($('.quantia').val());
       let AQprecoUnitario = parseFloat($('.total_preco').data('preco'));

        if ($(this).text() == '-') 
        {
            if (AQquantia != 1) {
                $('.quantia').val(AQquantia - 1);
                $('.total_preco').text('R$ ' + ((AQquantia - 1) * AQprecoUnitario).toFixed(2).replace('.', ','));
            }
        }
        else 
        {
            $('.quantia').val(AQquantia + 1);
            $('.total_preco').text('R$ ' + ((AQquantia + 1) * AQprecoUnitario).toFixed(2).replace('.', ','));
        }
    });

    $('.quantia').on('input', function () {
       let AQquantia = parseInt($(this).val());
       let AQprecoUnitario = parseFloat($('.total_preco').data('preco'));

        $('.total_preco').text('R$ ' + (AQquantia * AQprecoUnitario).toFixed(2).replace('.', ','));
    });
}




/**
 *  EXPLICAÇÃO:
 * 
 *   A função EnviarProduto adiciona um evento de clique ao elemento com a classe 'enviar_produto'.
 *   Quando esse elemento é clicado, obtém o id do produto do elemento com a classe 'descricao_produto',
 *   clona o produto original, modifica a classe e adiciona um ícone de exclusão.
 *   Em seguida, adiciona o produto clonado ao carrinho para cada quantidade do produto.
 *   Por fim, exibe todos os produtos e oculta a descrição do produto.
 * 
 * */
function EnviarProduto() {
    $('.enviar_produto').on('click', function () {
       let EPprodutoId = $('.descricao_produto').data('produto_id');
       let EPprodutoOriginal = $('.produtos .itens[data-produto_id="' + EPprodutoId + '"]');
       let EPcarrinho = $('.carrinho .content');
       let EPquantidadeProdutos = $('.quantia').val();

       for (let i = 0; i < EPquantidadeProdutos; i++) {
           let EPprodutoCopia = EPprodutoOriginal.clone();
            EPprodutoCopia.removeClass('itens').addClass('itens_carrinho');
            EPprodutoCopia.prepend('<span class="material-icons">delete</span>');
            EPprodutoCopia.show();
            EPcarrinho.append(EPprodutoCopia);
        }

        $('.produtos .itens').show();
        $('.descricao_produto').hide();
    });
}





/**
 *  EXPLICAÇÃO:
 * 
 *   A função EnviarCarrinho adiciona um evento de clique ao elemento com a classe 'enviar_comanda'.
 *   Quando esse elemento é clicado, coleta os ids dos produtos no carrinho, calcula o total, a taxa e o valor restante,
 *   e faz uma requisição POST para o endpoint '/postComanda' com essas informações.
 *   Em caso de sucesso, atualiza os valores na interface do usuário.
 * 
 * */
function EnviarCarrinho() {
    $('.enviar_comanda').on('click', function () {
    
        let ECids = [];
        $(".carrinho .itens_carrinho").each(function () {
           let ECproduto_id = $(this).data('produto_id');
            ECids.push(ECproduto_id);
            $(this).appendTo(".comanda .content");
        });

        $(".carrinho .content").empty();

       let ECidString = ECids.join(",");
       let ECstringProdutos = $('.string_produtos');

       if (ECstringProdutos.val() != '') {
            ECidString += ',' + ECstringProdutos.val()
        }

       let ECnomeVariavel, ECvalorVariavel;

       ECnomeVariavel = $('.tipo').val();
       ECvalorVariavel = Number($('.id_tipo').val());

       let ECtotal = 0;
        $(".comanda .content .itens_carrinho .preco").each(function () {
           let ECpreco = $(this).text().replace('R$', '').trim();
            ECtotal += parseFloat(ECpreco);
        });

       let ECpago = parseFloat($(".v_pago span").text().replace('R$', '').trim());
       let ECtotal_taxa = ECtotal * 1.1;
       let ECrestante; 
       if(localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'true')
       {
           ECrestante = ECtotal - ECpago; 
       }
       else
       {
         ECrestante = ECtotal_taxa - ECpago; 
       }
       
       let ECtaxa = ECtotal_taxa - ECtotal;

        $.ajax({
            type: 'POST',
            url: '/postComanda',
            data: JSON.stringify({
                idString: ECidString,
                total: ECtotal,
                total_taxa: ECtotal_taxa,
                taxa: ECtaxa,
                restante: ECrestante,
                nomeVariavel: ECnomeVariavel,
                valorVariavel: ECvalorVariavel
            }),
            contentType: 'application/json',
            success: function (response) {
                // Em caso de sucesso, atualiza os valores na interface do usuário.
                ECstringProdutos.val(ECidString);
                $(".v_total span").first().text("R$ " + ECtotal.toFixed(2));
                $(".v_tx_servico span").first().text("R$ " + ECtaxa.toFixed(2));
                $(".v_total_tx span").first().text("R$ " + ECtotal_taxa.toFixed(2));
                $(".v_pago span").first().text("R$ " + parseFloat($(".v_pago span").text().replace('R$', '').trim()).toFixed(2));
                $(".v_resto span").first().text("R$ " + ECrestante.toFixed(2));
            },
            error: function (xhr) {
                // Em caso de erro, exibe um alerta com o status da requisição.
                alert('Request failed.  Returned status of ' + xhr.status);
            }
        });
    });
}






/**
 *  EXPLICAÇÃO:
 * 
 *   A função CarregaComanda é chamada quando a página é carregada. Ela faz uma requisição POST para o endpoint '/getComanda'
 *   para obter a lista de produtos do banco de dados. Essa lista é então colocada no input com a classe '.string_produtos'.
 *   Além disso, ela obtém asletiáveis 'mesa_id' ou 'balcao_id' da URL e as envia na requisição.
 *   Quando a resposta é recebida, a função atualiza os valores na interface do usuário e coloca os produtos na comanda.
 * 
 * */
function CarregaComanda() {

   let CCnomeVariavel, CCvalorVariavel;

   CCnomeVariavel = $('.tipo').val();
   CCvalorVariavel = Number($('.id_tipo').val());

    $.ajax({
        type: 'POST',
        url: '/getComanda',
        data: JSON.stringify({
            nomeVariavel: CCnomeVariavel,
            valorVariavel: CCvalorVariavel
        }),
        contentType: 'application/json',
        success: function (response) {
            // Em caso de sucesso, atualiza os valores na interface do usuário.
            $('.string_produtos').val(response[0].produtos);
            $(".v_total span").first().text("R$ " + response[0].total);
            $(".v_tx_servico span").first().text("R$ " + response[0].taxa);
            $(".v_total_tx span").first().text("R$ " + response[0].total_taxa);
            $(".v_pago span").first().text("R$ " + response[0].pago);
            

            if(localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'true'){
                $(".v_resto span").first().text("R$ " + (response[0].total - response[0].pago).toFixed(2));
            }
            else{
                $(".v_resto span").first().text("R$ " + (response[0].total_taxa - response[0].pago).toFixed(2));
            }

            // Coloca os produtos na comanda.
            setTimeout(function () {
               let CCvetorProdutos = $('.string_produtos').val().split(',').map(Number);
                $.each(CCvetorProdutos, function (i, val) {
                   let CCproduto = $('.produtos .content [data-produto_id="' + val + '"]');
                    if (CCproduto.length) {
                       let CCcopia = CCproduto.clone();
                       CCcopia.removeClass('itens');
                       CCcopia.addClass('itens_carrinho');
                       CCcopia.prepend('<span class="material-icons">delete</span>');
                        $('.comanda .content').append(CCcopia);
                    }
                });

               let CCstringPagamentos = response[0].pagamentos;

                if(CCstringPagamentos != '')
                {
                   let CCarrayPagamentos = CCstringPagamentos.replace(/;+$/, '').split(';');
                   let CCarrayValoresPagos = [];
                    for (var i = 0; i < CCarrayPagamentos.length; i++) {
                       let CCvalorString = CCarrayPagamentos[i].split('- R$ ')[1]; // Pega a parte após '- R$ '
                       CCvalorString = CCvalorString.replace(',', '.');
                       let CCvalorNumerico = parseFloat(CCvalorString);
                       CCarrayValoresPagos.push(CCvalorNumerico);
                    }

                    for (var i = 0; i < CCarrayPagamentos.length; i++) {
                        $(".lista_pagamentos").append('<li data-valorPago=' + CCarrayValoresPagos[i] +'>' + CCarrayPagamentos[i] + '<i class="tiny removePagamento material-icons">remove</i></li>');
                    }

                    $('.string_pagamentos').val(CCstringPagamentos);
                }
                else
                {
                    $('.string_pagamentos').val(CCstringPagamentos);
                }
            }, 300);
        },
        error: function (xhr) {
            alert('poha');
        }
    });
}





/**
 *  EXPLICAÇÃO:
 * 
 *   A função ExcluiProduto adiciona eventos de clique aos elementos com a classe 'itens_carrinho' dentro dos elementos com as classes      
 *  'comanda .content' e 'carrinho .content'.
 *   Quando um desses elementos é clicado, o produto é removido, os ids dos produtos restantes são coletados e o total, a taxa e o valor    
 *   restante são recalculados.
 *   Em seguida, faz uma requisição POST para o endpoint '/postComanda' com essas informações.
 *   Em caso de sucesso, atualiza os valores na interface do usuário.
 * 
 * */
function ExcluiProduto() {
    $('.comanda .content').on('click', '.itens_carrinho span', function () {

        $(this).parent().remove();

        let EPids = [];

       $(".comanda .itens_carrinho").each(function () {
           let EPproduto_id = $(this).data('produto_id');
           EPids.push(EPproduto_id);
        });

       let EPidString = EPids.join(",");
      
       let EPnomeVariavel, EPvalorVariavel;

       EPnomeVariavel = $('.tipo').val();
       EPvalorVariavel = Number($('.id_tipo').val());

       let EPtotal = 0;
        $(".comanda .content .itens_carrinho .preco").each(function () {
           let EPpreco = $(this).text().replace('R$', '').trim();
           EPtotal += parseFloat(EPpreco);
        });
       let EPtotal_taxa = EPtotal * 1.1;
       let EPrestante;
       if(localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'true'){
        EPrestante = EPtotal - parseFloat($(".v_pago span").text().replace('R$', '').trim());
       }
       else{
        EPrestante = EPtotal_taxa - parseFloat($(".v_pago span").text().replace('R$', '').trim());
       }
       let EPtaxa = EPtotal_taxa - EPtotal;
       let EPstringProdutos = $('.string_produtos');

        $.ajax({
            type: 'POST',
            url: '/postComanda',
            data: JSON.stringify({
                idString: EPidString,
                total: EPtotal,
                total_taxa: EPtotal_taxa,
                taxa: EPtaxa,
                restante: EPrestante,
                nomeVariavel: EPnomeVariavel,
                valorVariavel: EPvalorVariavel
            }),
            contentType: 'application/json',
            success: function (response) {
                EPstringProdutos.val(EPidString);
                $(".v_total span").first().text("R$ " + EPtotal.toFixed(2));
                $(".v_tx_servico span").first().text("R$ " + EPtaxa.toFixed(2));
                $(".v_total_tx span").first().text("R$ " + EPtotal_taxa.toFixed(2));
                $(".v_resto span").first().text("R$ " + EPrestante.toFixed(2));
            },
            error: function (xhr) {
                alert('Request failed.  Returned status of ' + xhr.status);
            }
        });
    });

    // Adiciona um evento de clique aos elementos com a classe 'itens_carrinho' dentro do elemento com a classe 'carrinho .content'.
    $('.carrinho .content').on('click', '.itens_carrinho span', function () {
        $(this).parent().remove();
    });
}






function PagarParcial() {

    $('.valor_a_pagar').mask('000.000.000.000.000,00', {reverse: true}).change(function() {
       let PPcurrent = $(this).val();
        if (PPcurrent.indexOf('R$') !== 0) {
            $(this).val('R$ ' + PPcurrent);
        }
    });
    
    $('.adicionar_pagamento').on('click' , function(){

        if ($('.comanda .content').is(':empty'))
        {
            alert("Não é possível adicionar pagamentos sem produtos na comanda!");
            return;
        } 

       let PPvalorAPagar = parseFloat($('.valor_a_pagar').val().replace('R$', '').replace('.', '').replace(',', '.')).toFixed(2);
       let PPrestante = parseFloat($(".v_resto span").text().replace('R$', '').trim());

       PPrestante = PPrestante - PPvalorAPagar;

       let PPstringPagamento = $('.browser-default').find("option:selected").text() + ' - ' + $('.valor_a_pagar').val().replace('.', '');

       let PPvaloPago = PPstringPagamento.replace(';', '').split('- R$ ')[1].replace(',', '.');

        $(".lista_pagamentos").append('<li data-valorPago = "' + parseFloat(PPvaloPago) + '">' + PPstringPagamento + '<i class="tiny removePagamento material-icons">remove</i></li>');

        $('.valor_a_pagar').val('');
        $('.browser-default').val('');
        PPstringPagamento += ';';
        
        if($('.string_pagamentos').val() != '')
        {
            PPstringPagamento += $('.string_pagamentos').val();
        }

        $('.string_pagamentos').val($('.string_pagamentos').val() + PPstringPagamento);


       let PPnomeVariavel, PPvalorVariavel;

       PPnomeVariavel = $('.tipo').val();
       PPvalorVariavel = Number($('.id_tipo').val());

       let PPsoma = 0;
        $(".lista_pagamentos li").each(function() {
           let PPvalorPago = $(this).data('valorpago'); // use 'valorpago' instead of 'valorPago'
            PPsoma += parseFloat(PPvalorPago);
        });
        
        $.ajax({
            type: 'POST',
            url: '/pagamento',
            data: JSON.stringify({
                stringPagamento: PPstringPagamento,
                soma: PPsoma,
                restante: PPrestante,
                nomeVariavel: PPnomeVariavel,
                valorVariavel: PPvalorVariavel
            }),
            contentType: 'application/json',
            success: function (response) {
                // Em caso de sucesso, atualiza os valores na interface do usuário.
                $(".v_pago span").first().text("R$ " + PPsoma.toFixed(2));
                $(".v_resto span").first().text("R$ " + PPrestante.toFixed(2));
            },
            error: function (xhr) {
                // Em caso de erro, exibe um alerta com o status da requisição.
                alert('Request failed.  Returned status of ' + xhr.status);
            }
        });
     
    });
}






function ExcluiPagamento(){
    
    $(document).on('click', '.lista_pagamentos li .removePagamento', function() {

        let EPAvalorPago = $(this).parent().data('valorpago');
        $(this).parent().remove();
        
        let EPAnomeVariavel, EPAvalorVariavel;

        EPAnomeVariavel = $('.tipo').val();
        EPAvalorVariavel = Number($('.id_tipo').val());
        
        let EPAtextoConcatenado = '';
        let EPAsoma = 0;
        $(".lista_pagamentos li").each(function() {
            let EPAvalorPago = $(this).data('valorpago'); // use 'valorpago' instead of 'valorPago'
            EPAtextoConcatenado += $(this).text() + ';';
            EPAsoma += parseFloat(EPAvalorPago);
        });

        let EPArestante = parseFloat($(".v_resto span").text().replace('R$', '').trim());

        EPArestante = EPArestante + EPAvalorPago;
        
        $.ajax({
           type: 'POST',
           url: '/pagamento',
           data: JSON.stringify({
               stringPagamento: EPAtextoConcatenado,
               soma: EPAsoma,
               restante: EPArestante,
               nomeVariavel: EPAnomeVariavel,
               valorVariavel: EPAvalorVariavel
           }),
           contentType: 'application/json',
           success: function (response) {
               // Em caso de sucesso, atualiza os valores na interface do usuário.
               $(".v_pago span").first().text("R$ " + EPAsoma.toFixed(2));
               $(".v_resto span").first().text("R$ " + EPArestante.toFixed(2));
           },
           error: function (xhr) {
               // Em caso de erro, exibe um alerta com o status da requisição.
               alert('Request failed.  Returned status of ' + xhr.status);
           }
       });
    
     });
}





//preciso adicionar uma validação para verificar se foi feito o pagamento ou se a comanda está zerada ou se possui pagamentos com a comanda zerada.
function FecharComanda() {

    $('.fechar_comanda').on('click', function(){

        if ($('.comanda .content').is(':empty'))
        {
            window.location.href = '../' + $('.tipo').val().replace('_id', '') + '/' + $('.tipo').val().replace('_id', '') + '.html';
        }
        else{
            let valorTotal = parseFloat($('.v_total span').text().replace('R$', '').replace(',', '.'));
            let valorPago = parseFloat($('.v_pago  span').text().replace('R$', '').replace(',', '.'));
            let valorTotalTaxa = parseFloat($('.v_total_tx span').text().replace('R$', '').replace(',', '.'));

            if((valorTotal > valorPago  && localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'true') || (valorTotalTaxa > valorPago && localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'false' )){
                alert("Falta pagamentos a serem afetuados!");
                return
            }
        }
        let FCtotal;
        let FCpagamentos;
        let FCprodutos;

        let FCtipoAtendimento;
        let FCvalorVariavel;

        FCtipoAtendimento = $('.tipo').val();
        FCvalorVariavel = Number($('.id_tipo').val());
 
        $.ajax({
            type: 'POST',
            url: '/getComanda',
            data: JSON.stringify({
                nomeVariavel: FCtipoAtendimento,
                valorVariavel: FCvalorVariavel
            }),
            contentType: 'application/json',
            success: function (response) {
                console.log(response[0].pago);

                FCtotal = response[0].pago;
                FCpagamentos = response[0].pagamentos;
                FCprodutos = response[0].produtos;
            },
            error: function (xhr) {
                alert('erro');
            }
        });

        setInterval(function(){
            $.ajax({
                type: 'POST',
                url: '/postRegistro',
                data: JSON.stringify({
                    nomeVariavel: FCtipoAtendimento,
                    total: FCtotal,
                    pagamentos: FCpagamentos,
                    produtos: FCprodutos
                }),
                contentType: 'application/json',
                success: function (response) {
                },
                error: function (xhr) {
                    alert('poha2');
                }
            });
    
    
            $.ajax({
                type: 'POST',
                url: '/EncerraComanda',
                data: JSON.stringify({
                    nomeVariavel: FCtipoAtendimento,
                    valorVariavel: FCvalorVariavel
                }),
                contentType: 'application/json',
                success: function (response) {
                },
                error: function (xhr) {
                    alert('poha3');
                }
            });
    
            FCtipoAtendimento = FCtipoAtendimento.replace('_id', '');
            let randomParam = new Date().getTime(); // Gera um parâmetro baseado no timestamp atual
            window.location.href = '../' + FCtipoAtendimento + '/' + FCtipoAtendimento + '.html?refresh=' + randomParam;
        },300);
    });

}

function TaxaDeServico(){
    setTimeout(function(){
        let TXvalorTotal = parseFloat($('.v_total span').text().replace('R$', '').replace(',', '.'));
        let TXvalorPago = parseFloat($('.v_pago  span').text().replace('R$', '').replace(',', '.'));
        let TXvalorTotalTaxa = parseFloat($('.v_total_tx span').text().replace('R$', '').replace(',', '.'));
        
        if(localStorage.getItem('checkbox_' +  $('.id_tipo').val()) === 'true')
        {
            $('.v_total_tx').css('color', 'red');
            $('.v_tx_servico').css('color', 'red');
    
            $(".v_resto span").html("R$ " + (TXvalorTotal-TXvalorPago).toFixed(2));
        }

    },100);


    $('.checkboxTaxa .troca').change(function() {
        if(this.checked) {
            $('.v_total_tx').css('color', 'red');
            $('.v_tx_servico').css('color', 'red');
            localStorage.setItem('checkbox_' +  $('.id_tipo').val(), true);

            let TXvalorTotal = parseFloat($('.v_total span').text().replace('R$', '').replace(',', '.'));
            let TXvalorPago = parseFloat($('.v_pago  span').text().replace('R$', '').replace(',', '.'));
            
            $(".v_resto span").html("R$ " + (TXvalorTotal-TXvalorPago).toFixed(2));

        } else {
            $('.v_total_tx').css('color', 'white');
            $('.v_tx_servico').css('color', 'white');
            localStorage.setItem('checkbox_' +  $('.id_tipo').val(), false);
            let TXvalorPago = parseFloat($('.v_pago  span').text().replace('R$', '').replace(',', '.'));
            let TXvalorTotalTaxa = parseFloat($('.v_total_tx span').text().replace('R$', '').replace(',', '.'));
            $(".v_resto span").html("R$ " + (TXvalorTotalTaxa-TXvalorPago).toFixed(2));
        }
    });
}

function ImprimirParcial()
{
    //pego oque já está na comanda.    

    let produtos = {};

    $(".itens_carrinho").each(function() {
        let produto_nm = $(this).find(".produto_nm").text();
        let preco = parseFloat($(this).find(".preco").text().replace("R$ ", ""));
        
        if (produtos[produto_nm]) {
            produtos[produto_nm].count++;
            produtos[produto_nm].total += preco;
        } else {
            produtos[produto_nm] = { count: 1, total: preco };
        }
    });
    
    let result = 
    `
     _____ _____ ____  ____      _      ____   ___ ___ ____  
    |_   _| ____|  _ \\|  _ \\    / \\    |  _ \\ / _ \\_ _/ ___| 
      | | |  _| | |_) | |_) |  / _ \\   | | | | | | | |\\___ \\ 
      | | | |___|  _ <|  _ <  / ___ \\  | |_| | |_| | | ___) |
      |_| |_____|_| \\_\\_| \\_\\/_/   \\_\\ |____/ \\___/___|____/ 
                                                            


		                	PEDIDO :
    
    `;
    result += '\n';
    for (let produto_nm in produtos) {
        result += "-----------------------------\n";
        result += produtos[produto_nm].count + "x " + produto_nm + " - " + produtos[produto_nm].total.toFixed(2) + "\n";
    }
    
    //vou ter que fazer um post para um novo controller , que irá criar o meu arquivo.
}