/**
 * serial_protocol.cpp
 * -------------------
 * Raspberry Pi ↔ Arduino seri protokolü implementasyonu.
 */

#include "config.h"
#include "serial_protocol.h"

// Satır tamponu
static char		rxBuffer[64];
static uint8_t	rxIndex = 0;

void	serialProtocolInit(void)
{
	Serial.begin(SERIAL_BAUD_RATE);
	while (!Serial)
	{;}
	memset(rxBuffer, 0, sizeof(rxBuffer));
	rxIndex = 0;
}
bool	serialReadCommand(Command &cmd)
{
	char	c;
	String	line;
	int		firstColon;
	int		secondColon;
	uint8_t	col;
	uint8_t	row;
	int		firstColon;
	int		secondColon;
	uint8_t	col;
	uint8_t	row;

	cmd = {CommandType::NONE, 0, 0, false};
	while (Serial.available() > 0)
	{
		c = (char)Serial.read();
		if (c == '\n' || c == '\r')
		{
			rxBuffer[rxIndex] = '\0';
			if (rxIndex > 0)
			{

				line = String(rxBuffer);
				line.trim();
				rxIndex = 0;
				memset(rxBuffer, 0, sizeof(rxBuffer));
				if (line.startsWith(CMD_STORE))
				{
					firstColon = line.indexOf(':');
					secondColon = line.indexOf(':', firstColon + 1);
					if (firstColon > 0 && secondColon > firstColon)
					{
						col = (uint8_t)line.substring(firstColon + 1,
								secondColon).toInt();
						row = (uint8_t)line.substring(secondColon + 1).toInt();
						cmd = {CommandType::STORE, col, row, true};
					}
					else
					{
						cmd.type = CommandType::UNKNOWN;
						cmd.valid = false;
					}
					return (true);
				}
				else if (line.startsWith(CMD_RETRIEVE))
				{
					firstColon = line.indexOf(':');
					secondColon = line.indexOf(':', firstColon + 1);
					if (firstColon > 0 && secondColon > firstColon)
					{
						col = (uint8_t)line.substring(firstColon + 1,
								secondColon).toInt();
						row = (uint8_t)line.substring(secondColon + 1).toInt();
						cmd = {CommandType::RETRIEVE, col, row, true};
					}
					else
					{
						cmd.type = CommandType::UNKNOWN;
						cmd.valid = false;
					}
					return (true);
				}
				else if (line.equals(CMD_HOME))
				{
					cmd = {CommandType::HOME, 0, 0, true};
					return (true);
				}
				else if (line.equals(CMD_STATUS))
				{
					cmd = {CommandType::STATUS, 0, 0, true};
					return (true);
				}
				else if (line.length() > 0)
				{
					cmd = {CommandType::UNKNOWN, 0, 0, false};
					return (true);
				}
			}
			else
				rxIndex = 0;
		}
		else
		{
			if (rxIndex < (sizeof(rxBuffer) - 1))
				rxBuffer[rxIndex++] = c;
		}
	}
	return (false);
}


void	serialSendReady(void)
{
	Serial.println(F(RESP_READY));
}

void	serialSendBusy(void)
{
	Serial.println(F(RESP_BUSY));
}

void	serialSendOK(const char *msg)
{
	Serial.print(F(RESP_OK));
	if (msg && msg[0] != '\0')
	{
		Serial.print(':');
		Serial.print(msg);
	}
	Serial.println();
}

void	serialSendError(const char *msg)
{
	Serial.print(F(RESP_ERROR));
	Serial.print(':');
	Serial.println(msg);
}
