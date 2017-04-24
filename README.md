# UI_Server_GatewaySMS
Servidor HTTP en C# el cual escucha constantemente un puerto dado y al recibir un POST, crea un nuevo Thread que lo maneja. 
Este Thread recibe los parametros de un mensaje y lo encola. 
Otro Thread se encarga de desencolar los mensajes y enviarlos por un modulo GSM mediante el puerto serial

Los parametros a configurar para su funcionamiento son:

En el archivo TCPServer, hay que setear la IP local de la PC en la variable "DEFAULT_SERVER" y el puerto en "DEFAULT_PORT"
El BaudRate para comunicarse con el GSM esta configurado en 115200, se puede editar en la clase GSM_Module, en la linea "this.serialPort.BaudRate=115200;" 
