// Entrega de arquivos que o backend só serve com token (não há link público): o PDF chega como Blob
// por uma requisição autenticada, e daqui ele é baixado ou aberto numa aba.

// Quanto tempo o endereço temporário do Blob fica vivo depois de entregue ao navegador. O download
// e o visualizador de PDF já leram o conteúdo nesse intervalo; passar disso só prende memória.
const VALIDADE_URL_MS = 60_000

function criarUrlTemporaria(blob: Blob): string {
  const url = URL.createObjectURL(blob)
  setTimeout(() => URL.revokeObjectURL(url), VALIDADE_URL_MS)
  return url
}

// Dispara o download com o nome informado (o `download` do <a> é quem define o nome do arquivo).
export function baixarArquivo(blob: Blob, nomeArquivo: string): void {
  const link = document.createElement('a')
  link.href = criarUrlTemporaria(blob)
  link.download = nomeArquivo
  link.rel = 'noopener'
  document.body.appendChild(link)
  link.click()
  link.remove()
}

// Precisa ser chamada de forma síncrona dentro do clique do usuário: o PDF só chega depois de uma
// requisição, e os navegadores (Safari e celulares em especial) bloqueiam uma aba aberta depois
// dessa espera. `null` = o navegador bloqueou o pop-up.
export function abrirAbaEmBranco(): Window | null {
  return window.open('', '_blank')
}

// Aponta a aba aberta antes (`abrirAbaEmBranco`) para o PDF que acabou de chegar.
export function mostrarArquivoNaAba(aba: Window, blob: Blob): void {
  aba.location.href = criarUrlTemporaria(blob)
}
