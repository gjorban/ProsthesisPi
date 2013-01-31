#include <fcntl.h>
#include <math.h>

const char* kPortName = "/dev/spidev0.0";
char* kGammaTable = 0;
#define STRIP_SIZE 64
#define PI 3.1415
#define DEG_2_RAD(x) (x * PI / 180.0f)

void BuildGammaTable()
{
	if (kGammaTable != 0)
	{
		free(kGammaTable);
	}
	
	kGammaTable = (char*)malloc(sizeof(char) * 256);
	
	int i = 0;
	for (i = 0; i < 256; ++i)
	{
		kGammaTable[i] = 0x80 | (char)floor(pow((float)i / 255.0f, 2.5f) * 127.0f + 0.5f);
	}
}

void SetPixel(char *ledStrip, char r, char b, char g, int index)
{
	ledStrip[index * 3] = kGammaTable[g];
	ledStrip[index * 3 + 1] = kGammaTable[r];
	ledStrip[index * 3 + 2] = kGammaTable[b];
}

void RandomizeStrip(char *ledStrip)
{
	int i = 0;
	for (i = 0; i < STRIP_SIZE; ++i)
	{
		SetPixel(ledStrip, kGammaTable[(char)rand()], kGammaTable[(char)rand()], kGammaTable[(char)rand()], i);
	}
}

int main (int argc, char ** argv)
{
	int i = 0;
	int dir = 1;
	//Open port 
	int fd = open(kPortName, O_RDWR | O_SYNC);
	
	//Amt of memory for the strip
	int stripBuffSize = sizeof(char) * 3 * STRIP_SIZE + 1;
	char* strip = (char*)malloc(stripBuffSize);
	
	printf("Port id is %i. Building gamma table\n", fd);	
	//Build gamma table
	BuildGammaTable();
	printf("Gamma table generation complete\n");
	
	//Clear the strip
	memset(strip, 0, stripBuffSize);
	
	if (fd != -1)
	{
		int j = 0;
		int k = 0;
		for (;;)
		{
			//Write the colour buffer to the SPI port
			write(fd, strip, stripBuffSize);
			
			//Control direction of cosine wave. Pause at extrema
			if (j == STRIP_SIZE - 1)
			{
				dir = -1;
				usleep(100000);
			}
			else if (j == 0)
			{
				dir = 1;
				usleep(100000);
			}
			
			j += dir;
			usleep(16000);
			//RandomizeStrip(strip);
			
			//Update the colour buffer
			for (i = 0; i < STRIP_SIZE; ++i)
			{
				float val = cos(((float)(j % STRIP_SIZE) - (float)i) / (float)(STRIP_SIZE / 2.0f) * PI / 2.0f);
				if (val < 0.0f)
				{
					val = 0.0f;
				}
				SetPixel(strip, (char)round(pow(val, 3.0f) * 255.0f), 0, 0, i);
			}
		}
		//Close port
		close(fd);
	}
	else
	{
		printf("%s isn't a valid SPI port", kPortName);
	}
	
	//Free memory
	if (kGammaTable != 0)
	{
		free(kGammaTable);
	}
	
	free(strip);
	return 0;
}