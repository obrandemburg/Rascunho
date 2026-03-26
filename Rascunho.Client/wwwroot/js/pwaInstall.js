// Localização: Rascunho.Client/wwwroot/js/pwaInstall.js
//
// Gerencia a lógica de instalação do PWA (Progressive Web App).
//
// FLUXO GERAL:
//   1. Um script inline em index.html captura o evento 'beforeinstallprompt'
//      o mais cedo possível, antes mesmo do Blazor inicializar.
//   2. Este módulo disponibiliza funções que o componente Blazor (PwaBaixarApp.razor)
//      chama via JS Interop para checar o estado e acionar a instalação.
//
// COMPATIBILIDADE:
//   - Android (Chrome/Edge): usa o evento nativo 'beforeinstallprompt' para
//     exibir o prompt de instalação do sistema operacional.
//   - iOS (Safari): não suporta 'beforeinstallprompt'. Exibe instruções manuais
//     via alert no componente Blazor.

window.PwaInstall = {

    // Verifica se o app JÁ está rodando como PWA instalado (fora do browser).
    // Retorna true quando o usuário abriu o app pela tela inicial, não pelo browser.
    isStandalone: function () {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    },

    // Verifica se o acesso vem de um dispositivo móvel.
    // Combina detecção por User Agent (Android/iOS/etc.) e por largura de tela (<= 768px).
    isMobile: function () {
        const byUserAgent = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
        const byScreenWidth = window.innerWidth <= 768;
        return byUserAgent || byScreenWidth;
    },

    // Verifica se o dispositivo é iOS.
    // iOS/Safari não dispara 'beforeinstallprompt', então tratamos separadamente.
    isIos: function () {
        return /iPhone|iPad|iPod/i.test(navigator.userAgent) && !window.MSStream;
    },

    // Verifica se o evento de instalação nativo foi capturado (Android/Chrome/Edge).
    // O evento é capturado pelo script inline no <head> do index.html.
    isInstallable: function () {
        return window._pwaInstallEvent !== null && window._pwaInstallEvent !== undefined;
    },

    // Avalia se o botão "Baixar App" deve ser exibido.
    // Condições para true:
    //   1. Não está rodando como PWA instalado (está no browser)
    //   2. Está em um dispositivo móvel
    //   3. É iOS (mostrará instruções manuais) OU tem evento de instalação pronto (Android)
    shouldShowButton: function () {
        const standalone = window.PwaInstall.isStandalone();
        const mobile = window.PwaInstall.isMobile();
        const ios = window.PwaInstall.isIos();
        const installable = window.PwaInstall.isInstallable();

        return !standalone && mobile && (ios || installable);
    },

    // Aciona o prompt nativo de instalação do sistema operacional (Android/Chrome/Edge).
    // Retorna true se o usuário aceitou instalar, false se recusou ou o evento não estava disponível.
    // Chamado pelo componente PwaBaixarApp.razor quando o usuário clica em "Baixar App".
    promptInstall: async function () {
        if (!window.PwaInstall.isInstallable()) {
            console.warn('[PwaInstall] Evento de instalação não disponível.');
            return false;
        }

        window._pwaInstallEvent.prompt();
        const result = await window._pwaInstallEvent.userChoice;
        window._pwaInstallEvent = null; // Limpa após uso — o evento só pode ser usado uma vez
        return result.outcome === 'accepted';
    }
};
