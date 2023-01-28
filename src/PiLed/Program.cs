using Iot.Device.Adc;
using Iot.Device.CharacterLcd;
using Iot.Device.Pcx857x;
using Iot.Device.Pn532;
using Iot.Device.Pn532.ListPassive;
using Iot.Device.Spi;
using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Drawing.Printing;
using System.Threading;

namespace PiLed;
public class Program
{
    public static void Main(string[] args)
    {
        using I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
        using var driver = new Pcf8574(i2c);
        using var ledController = new GpioController();
        var redLed = new Led(21, ledController);
        var greenLed = new Led(16, ledController);
        using (var lcd = new Lcd2004(registerSelectPin: 0,
            enablePin: 2,
            dataPins: new int[] { 4, 5, 6, 7 },
            backlightPin: 3,
            backlightBrightness: 0.1f,
            readWritePin: 1,
            new GpioController(PinNumberingScheme.Logical, driver)))
        {
            var lcdWriter = new Lcd2004Writer(lcd);
            lcdWriter.Write("Blinking LEDs. Press ctrl+c to end.");
            redLed.On();
            while (true)
            {
                Thread.Sleep(1000);
                lcdWriter.ShiftCurrentTextLeft(4);
                redLed.Toggle();
                greenLed.Toggle();
            }
        }
    }
}

public class Lcd2004Writer
{
    private readonly Lcd2004 _lcd;
    private int _currentLine = 0;
    private int _currentTextIndex = 0;
    private string _currentText = string.Empty;

    public Lcd2004Writer(Lcd2004 lcd)
    {
        _lcd = lcd;
        ClearScreen();
    }

    public void Write(string text)
    {
        _currentText = text;
        _lcd.Write(_currentText);
        _currentLine = (_currentLine == 3) ? 0 : _currentLine + 1;
    }

    public void ShiftCurrentTextLeft(int numOfCharsToShift = 1)
    {
        _currentTextIndex += numOfCharsToShift;
        if (_currentTextIndex >= _currentText.Length)
        {
            _currentTextIndex = 0;
        }
        _lcd.Write(_currentText);
        _currentLine = (_currentLine == 3) ? 0 : _currentLine + 1;
        var textToWrite = _currentText.Substring(_currentTextIndex);
        ClearScreen();
        _lcd.Write(textToWrite);
        _currentLine = (_currentLine == 3) ? 0 : _currentLine + 1;
    }

    public void ClearScreen()
    {
        _lcd.Clear();
        _currentLine = 0;
        _lcd.SetCursorPosition(0, _currentLine);
    }
}

public class Led
{
    public int Pin { get; init; }
    private readonly PinMode _pinMode;
    private PinValue _pinValue { get; set; }
    private readonly GpioController _controller;

    public Led(int pin, GpioController controller)
    {
        Pin = pin;
        _pinMode = PinMode.Output;
        _pinValue = PinValue.Low;
        _controller = controller;
        controller.OpenPin(Pin, _pinMode);
        controller.Write(Pin, _pinValue);
    }

    public bool IsOn => _pinValue == PinValue.High;

    public void On()
    {
        _pinValue = PinValue.High;
        Write();
    }

    public void Off()
    {
        _pinValue = PinValue.Low;
        Write();
    }

    public void Toggle()
    {
        if (IsOn)
        {
            Off();
            return;
        }
        On();
    }

    private void Write()
    {
        _controller.Write(Pin, _pinValue);
    }
}