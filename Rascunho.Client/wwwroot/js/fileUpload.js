// Localização: Rascunho.Client/wwwroot/js/fileUpload.js
//
// Este arquivo expõe funções JavaScript que o Blazor não consegue fazer nativamente.
// Blazor WASM roda em WebAssembly — para interagir com o DOM do navegador
// (como acionar um clique em um <input type="file">), precisamos chamar JS.
//
// Como funciona o fluxo:
//   1. Usuário clica no botão "Escolher Foto" no Blazor
//   2. Blazor chama JS.InvokeVoidAsync("fileUpload.abrirSeletor", "inputFoto")
//   3. Esta função JavaScript faz document.getElementById('inputFoto').click()
//   4. O seletor de arquivos do sistema operacional abre
//   5. Usuário escolhe a foto → dispara o evento OnChange do InputFile no Blazor

window.fileUpload = {
    /**
     * Aciona programaticamente um input[type=file] pelo seu id.
     * @param {string} inputId - o id do elemento InputFile no DOM
     */
    abrirSeletor: function (inputId) {
        const input = document.getElementById(inputId);
        if (input) {
            input.click();
        } else {
            console.warn(`[fileUpload] Elemento #${inputId} não encontrado.`);
        }
    }
};
