# Assinador SIG para Windows

Aplicativo local para assinatura de documentos PDF com certificados digitais ICP-Brasil A1 e A3 disponíveis no Windows.

## Segurança

- O certificado e a chave privada não são exportados, copiados ou enviados ao SIG.
- A assinatura é executada pelo provedor criptográfico do Windows ou pelo driver do token/cartão A3.
- Nenhuma senha ou PIN é armazenado pelo aplicativo.
- O aplicativo funciona sem privilégios de administrador.

## Primeira versão de teste

1. Abra `AssinadorSIG.exe` em um computador Windows 10 ou 11 de 64 bits.
2. Clique em **Selecionar certificado**.
3. Escolha um certificado válido com chave privada.
4. Selecione um PDF e clique em **Assinar documento**.
5. Salve o novo arquivo assinado e confira a assinatura no leitor de PDF.

Para certificados A3, instale antes o driver fornecido pela autoridade certificadora e conecte o token ou cartão.

> Esta versão valida primeiro a assinatura local. A integração automática com pagamento, envio e download pelo site será ativada depois do teste com certificado real.
