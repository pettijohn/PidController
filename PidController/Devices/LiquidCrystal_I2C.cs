using System.Threading;
using Toolbox.NETMF;
using Toolbox.NETMF.Hardware;

/*
 * Adapted from LiquidCrystal_I2C.cpp from Arduino catalog
 * Added: 
 *  Text formatting and display methods
 *  Reference to Stefan Thoolen (http://www.netmftoolbox.com/) MultiI2C
 *      (Under separate license)
 * 
 * Copyright 2011-2012 Axel Granholm
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace PidController.Devices
{
    /// <summary>
    /// Class for managing the SainSmart 2004 LCD (SKU:20-011-913) with 
    /// integratead I2C controler.
    ///     Adapted from LiquidCrystal_I2C.cpp from Arduino catalog
    /// </summary>
    public class LiquidCrystal_I2C
    {
        private const int MAX_FREQ = 100;
        private byte _Addr;
        private byte _displayfunction;
        private byte _displaycontrol;
        private byte _displaymode;
        private byte _numlines;
        protected byte _cols;
        protected byte _rows;
        private byte _backlightval;

        private object LCDLock;

        private MultiI2C _I2C;

        // When the display powers up, it is configured as follows:
        //
        // 1. Display clear
        // 2. Function set: 
        //    DL = 1; 8-bit interface data 
        //    N = 0; 1-line display 
        //    F = 0; 5x8 dot character font 
        // 3. Display on/off control: 
        //    D = 0; Display off 
        //    C = 0; Cursor off 
        //    B = 0; Blinking off 
        // 4. Entry mode set: 
        //    I/D = 1; Increment by 1
        //    S = 0; No shift 
        //
        // Note, however, that resetting the Arduino doesn't reset the LCD, so we
        // can't assume that its in that state when a sketch starts (and the
        // LiquidCrystal constructor is called).

        #region "Constructors and Initializers"

        public LiquidCrystal_I2C(byte lcd_Addr, byte lcd_cols, byte lcd_rows)
        {
            LCDLock = new object();

            _Addr = lcd_Addr;
            _cols = lcd_cols;
            _rows = lcd_rows;
            _backlightval = DefineConstants.LCD_NOBACKLIGHT;
            _I2C = new MultiI2C(lcd_Addr, MAX_FREQ);
            begin(_cols, _rows);
        }

        protected void reset()
        {
            lock (LCDLock)
                begin(_cols, _rows);
        }

        /// <summary>
        /// Sends the initialization sequence for the device
        /// </summary>
        /// <param name="cols">Number of columns</param>
        /// <param name="lines">Number of rows</param>
        /// <param name="dotsize">Optional cursor size</param>
        protected void begin(byte cols, byte lines, byte dotsize = DefineConstants.LCD_5x8DOTS)
        {
            _displayfunction = DefineConstants.LCD_4BITMODE | DefineConstants.LCD_1LINE | DefineConstants.LCD_5x8DOTS;

            if (lines > 1)
            {
                _displayfunction |= DefineConstants.LCD_2LINE;
            }
            _numlines = lines;

            // for some 1 line displays you can select a 10 pixel high font
            if ((dotsize != 0) && (lines == 1))
            {
                _displayfunction |= DefineConstants.LCD_5x10DOTS;
            }

            // SEE PAGE 45/46 FOR INITIALIZATION SPECIFICATION!
            // according to datasheet, we need at least 40ms after power rises above 2.7V
            // before sending commands. Arduino can turn on way befer 4.5V so we'll wait 50
            //Thread.Sleep(50);

            // Now we pull both RS and R/W low to begin commands
            expanderWrite(_backlightval); // reset expander and turn backlight off (Bit 8 =1)
            Thread.Sleep(1000);

            //put the LCD into 4 bit mode
            // this is according to the hitachi HD44780 datasheet
            // figure 24, pg 46

            // we start in 8bit mode, try to set 4 bit mode
            write4bits(0x03 << 4);
            Thread.Sleep(8); // wait min 4.1ms

            // second try
            write4bits(0x03 << 4);
            Thread.Sleep(8); // wait min 4.1ms

            // third go!
            write4bits(0x03 << 4);
            Thread.Sleep(4);

            // finally, set to 4-bit interface
            write4bits(0x02 << 4);

            // set # lines, font size, etc.
            command((byte)(DefineConstants.LCD_FUNCTIONSET | _displayfunction));

            // turn the display on with no cursor or blinking default
            _displaycontrol = DefineConstants.LCD_DISPLAYON | DefineConstants.LCD_CURSOROFF | DefineConstants.LCD_BLINKOFF;
            display();

            // clear it off
            clear();

            // Initialize to default text direction (for roman languages)
            _displaymode = DefineConstants.LCD_ENTRYLEFT | DefineConstants.LCD_ENTRYSHIFTDECREMENT;

            // set the entry mode
            command((byte)(DefineConstants.LCD_ENTRYMODESET | _displaymode));

            home();
        }
        #endregion

        #region "High Level commands"
        /// <summary>
        /// clear display, set cursor position to zero
        /// </summary>
        protected void clear()
        {
            lock (LCDLock)
            {
                command(DefineConstants.LCD_CLEARDISPLAY);
                Thread.Sleep(2); // this command takes a long time!
            }
        }

        /// <summary>
        /// set cursor position to zero
        /// </summary>
        protected void home()
        {
            lock (LCDLock)
            {
                command(DefineConstants.LCD_RETURNHOME);
                Thread.Sleep(2); // this command takes a long time!
            }
        }

        /// <summary>
        /// Turn the display on/off (quickly)
        /// </summary>
        protected void noDisplay()
        {
            lock (LCDLock)
            {
                byte display = DefineConstants.LCD_DISPLAYON;
                _displaycontrol &= (byte)~display;
                command((byte)(DefineConstants.LCD_DISPLAYCONTROL | _displaycontrol));
            }
        }
        /// <summary>
        /// Turn display on
        /// </summary>
        protected void display()
        {
            lock (LCDLock)
            {
                _displaycontrol |= DefineConstants.LCD_DISPLAYON;
                command((byte)(DefineConstants.LCD_DISPLAYCONTROL | _displaycontrol));
            }
        }

        /// <summary>
        /// Turn off the blinking cursor
        /// </summary>
        protected void noBlink()
        {
            lock (LCDLock)
            {
                byte blink = DefineConstants.LCD_BLINKON;
                _displaycontrol &= (byte)~blink;
                command((byte)(DefineConstants.LCD_DISPLAYCONTROL | _displaycontrol));
            }
        }
        /// <summary>
        /// Turn on the blinking cursor
        /// </summary>
        protected void blink()
        {
            lock (LCDLock)
            {
                _displaycontrol |= DefineConstants.LCD_BLINKON;
                command((byte)(DefineConstants.LCD_DISPLAYCONTROL | _displaycontrol));
            }
        }

        /// <summary>
        ///Turns on the underline cursor
        /// </summary>
        protected void noCursor()
        {
            lock (LCDLock)
            {
                byte cursor = DefineConstants.LCD_CURSORON;
                _displaycontrol &= (byte)~cursor;
                command((byte)(DefineConstants.LCD_DISPLAYCONTROL | _displaycontrol));
            }
        }
        /// <summary>
        /// Turns off the underline cursor
        /// </summary>
        protected void cursor()
        {
            lock (LCDLock)
            {
                _displaycontrol |= DefineConstants.LCD_CURSORON;
                command((byte)(DefineConstants.LCD_DISPLAYCONTROL | _displaycontrol));
            }
        }

        /// <summary>
        /// Scroll display left without changing the RAM
        /// </summary>
        protected void scrollDisplayLeft()
        {
            lock (LCDLock)
                command(DefineConstants.LCD_CURSORSHIFT | DefineConstants.LCD_DISPLAYMOVE | DefineConstants.LCD_MOVELEFT);
        }
        /// <summary>
        /// Scroll display right without changing the RAM
        /// </summary>
        protected void scrollDisplayRight()
        {
            lock (LCDLock)
                command(DefineConstants.LCD_CURSORSHIFT | DefineConstants.LCD_DISPLAYMOVE | DefineConstants.LCD_MOVERIGHT);
        }

        /// <summary>
        ///This is for text that flows Left to Right 
        /// </summary>
        protected void leftToRight()
        {
            lock (LCDLock)
            {
                _displaymode |= DefineConstants.LCD_ENTRYLEFT;
                command((byte)(DefineConstants.LCD_ENTRYMODESET | _displaymode));
            }
        }

        /// <summary>
        ///This is for text that flows Right to Left 
        /// </summary>
        protected void rightToLeft()
        {
            lock (LCDLock)
            {
                byte entry = DefineConstants.LCD_ENTRYLEFT;
                _displaymode &= (byte)~entry;
                command((byte)(DefineConstants.LCD_ENTRYMODESET | _displaymode));
            }
        }

        /// <summary>
        /// Turn off the (optional) backlight
        /// </summary>
        protected void noBacklight()
        {
            lock (LCDLock)
            {
                _backlightval = DefineConstants.LCD_NOBACKLIGHT;
                expanderWrite(0);
            }
        }

        /// <summary>
        /// Turn on the (optional) backlight
        /// </summary>
        protected void backlight()
        {
            lock (LCDLock)
            {
                _backlightval = DefineConstants.LCD_BACKLIGHT;
                expanderWrite(0);
            }
        }

        /// <summary>
        /// This will 'right justify' text from the cursor
        /// </summary>
        protected void autoscroll()
        {
            lock (LCDLock)
            {
                _displaymode |= DefineConstants.LCD_ENTRYSHIFTINCREMENT;
                command((byte)(DefineConstants.LCD_ENTRYMODESET | _displaymode));
            }
        }

        /// <summary>
        /// This will 'left justify' text from the cursor
        /// </summary>
        protected void noAutoscroll()
        {
            lock (LCDLock)
            {
                byte entry = DefineConstants.LCD_ENTRYSHIFTINCREMENT;
                _displaymode &= (byte)~entry;
                command((byte)(DefineConstants.LCD_ENTRYMODESET | _displaymode));
            }
        }

        #endregion

        #region String commands

        /// <summary>
        /// Writes a string array to the device.  Truncates strings to the number of columns allowed, 
        ///  or wraps if enough room.
        /// </summary>
        /// <param name="data">string array with each element mapped to a line on the LCD.  Will 
        ///   truncate if too many are sent</param>
        /// <param name="format">C = center each line</param>
        private void write(string[] data, char format = ' ')
        {
            int NumRows = data.Length;
            if (NumRows > _rows) NumRows = _rows;
            //base.clear();

            //truncate the strings, and center if needed
            lock (LCDLock)
            {
                for (int i = 0; i < NumRows; i++)
                {
                    if (data[i].Length > _cols) data[i] = data[i].Substring(0, _cols);
                    if (data[i].Length != 0) write(data[i], 0, (byte)i, format);
                }
            }
        }

        /// <summary>
        /// Takes in a string, and breaks it up into four strings suitable for the display
        /// </summary>
        /// <param name="value">String to be converted</param>
        /// <returns>string array containing one element for each row on the display</returns>
        public string[] MakeTextBlock(string value)
        {
            int LastSpace = 0;
            string[] TextBlock = new string[_rows];
            for (int i = 0; i < _rows; i++) TextBlock[i] = "";

            for (int pass = 0; pass < _rows; pass++)
            {
                int SegLen = _cols;
                if (value.Length < LastSpace + _cols) SegLen = value.Length - LastSpace;

                int ThisSpace = 0;
                string part = value.Substring(LastSpace, SegLen);

                ThisSpace = part.Length;
                if (part.Length >= _cols)
                {
                    for (int i = 0; i < part.Length; i++) if (part[i] == ' ') ThisSpace = i;
                }

                TextBlock[pass] = part.Substring(0, ThisSpace);
                LastSpace += ThisSpace + 1;

                if (LastSpace >= value.Length) break;
            }

            return TextBlock;
        }

        /// <summary>
        /// Write the string at a specific location, with a specific formatting
        /// </summary>
        /// <param name="value">Value to display</param>
        /// <param name="col">Starting column </param>
        /// <param name="row">Row to display</param>
        /// <param name="format">C = Centered on the row</param>
        private void write(string value, byte col, byte row, char format = 'c')
        {
            string NewString = "";

            if ((format == 'c' || format == 'C') && value.Length < _cols && col == 0)
            {
                for (int space = 0; space < (_cols - value.Length) / 2; space++) NewString += " ";
                NewString = NewString + value;
            }
            else NewString = value;

            lock (LCDLock)
            {
                setCursor(col, row);
                write(NewString);
                //if (row == TEMPROW && NewString.Trim().Length != 0) LastTempLine = DateTime.Now;
            }
        }

        /// <summary>
        /// Send string to the current cursor position
        /// If the string is longer than number of columns, the string will be broken
        ///   up into parts and written across multiple lines.
        /// </summary>
        /// <param name="value">string value to display</param>
        public void write(string value)
        {
            if (value.Length > _cols)
            {
                write(MakeTextBlock(value), 'c');
            }
            else
            {
                byte[] Buffer = Tools.Chars2Bytes(value.ToCharArray());
                lock (LCDLock)
                    write(Buffer);
            }
        }

        #endregion

        #region "Charaters and writing"

        /// <summary>
        /// Allows us to fill the first 8 CGRAM locations with custom characters
        /// </summary>
        /// <param name="location"> 0-7 locations</param>
        /// <param name="charmap"> byte[8] containg charater map</param>
        protected void createChar(byte location, byte[] charmap)
        {
            //E.g., http://cdn.instructables.com/FLN/05VC/G68HDRBT/FLN05VCG68HDRBT.MEDIUM.jpg
            //http://www.instructables.com/id/LED-Scolling-Dot-Matrix-Font-Graphics-Generator-/
            lock (LCDLock)
            {
                location &= 0x7; // we only have 8 locations 0-7
                command((byte)(DefineConstants.LCD_SETCGRAMADDR | (location << 3)));
                for (int i = 0; i < 8; i++)
                {
                    write(charmap[i]);
                }
            }
        }

        /// <summary>
        /// Sets the location of the cursor prior to senting characters
        /// </summary>
        /// <param name="col"> 0 - MaxCols - 1</param>
        /// <param name="row"> 0 - MaxRows - 1</param>
        protected void setCursor(byte col, byte row)
        {
            lock (LCDLock)
            {
                int[] row_offsets = { 0x00, 0x40, 0x14, 0x54 };
                if (row > _numlines)
                {
                    row = (byte)(_numlines - 1); // we count rows starting w/0
                }
                command((byte)(DefineConstants.LCD_SETDDRAMADDR | (col + row_offsets[row])));
            }
        }



        public void write(byte[] value)
        {
            lock (LCDLock)
            {
                for (int position = 0; position < value.Length; position++)
                    write(value[position]);
            }
        }
        public void write(byte value)
        {
            lock (LCDLock)
                send(value, 0x01);
        }

        #endregion

        #region "Mid level commands"
        /*********** mid level commands, for sending data/cmds */

        private void command(byte value)
        {
            send(value, 0);
        }

        ////compatibility API function aliases
        public void blink_on()
        {
            blink();
        }
        public void blink_off()
        {
            noBlink();
        }
        public void cursor_on()
        {
            cursor();
        }
        public void cursor_off()
        {
            noCursor();
        }
        public void setBacklight(bool new_val)
        {
            if (new_val) backlight(); // turn backlight on
            else noBacklight(); // turn backlight off
        }
        public void load_custom_character(byte char_num, byte[] charMap)
        {
            createChar(char_num, charMap);
        }

        #endregion

        #region "Low Level commands"
        /// <summary>
        /// write either command or data to the devide
        /// </summary>
        /// <param name="value">byte to send</param>
        /// <param name="mode">0 = command, 1 = data</param>
        private void send(byte value, byte mode)
        {
            byte highnib = (byte)(value & 0xf0);
            byte lownib = (byte)((value << 4) & 0xf0);

            write4bits((byte)((highnib) | mode));
            write4bits((byte)((lownib) | mode));
        }
        private void write4bits(byte value)
        {
            expanderWrite(value);
            pulseEnable(value);
        }
        /// <summary>
        /// Lowest level command to send data to the I2C port
        /// </summary>
        /// <param name="_data"></param>
        private void expanderWrite(byte _data)
        {
            int length = _I2C.Write(new byte[] { (byte)(_data | _backlightval) });
            //if (length == 0)
            //    throw new System.IO.IOException("No data written to I2C device");
        }
        private void pulseEnable(byte _data)
        {
            expanderWrite((byte)(_data | 0x04)); // En high
            //Thread.Sleep(1); // enable pulse must be >450ns

            expanderWrite((byte)(_data & ~0x04)); // En low
            //Thread.Sleep(50); // commands need > 37us to settle
        }
        #endregion
    }

    internal static class DefineConstants
    {
        public const int LCD_CLEARDISPLAY = 0x01;
        public const int LCD_RETURNHOME = 0x02;
        public const int LCD_ENTRYMODESET = 0x04;
        public const int LCD_DISPLAYCONTROL = 0x08;
        public const int LCD_CURSORSHIFT = 0x10;
        public const int LCD_FUNCTIONSET = 0x20;
        public const int LCD_SETCGRAMADDR = 0x40;
        public const int LCD_SETDDRAMADDR = 0x80;
        public const int LCD_ENTRYRIGHT = 0x00;
        public const int LCD_ENTRYLEFT = 0x02;
        public const int LCD_ENTRYSHIFTINCREMENT = 0x01;
        public const int LCD_ENTRYSHIFTDECREMENT = 0x00;
        public const int LCD_DISPLAYON = 0x04;
        public const int LCD_DISPLAYOFF = 0x00;
        public const int LCD_CURSORON = 0x02;
        public const int LCD_CURSOROFF = 0x00;
        public const int LCD_BLINKON = 0x01;
        public const int LCD_BLINKOFF = 0x00;
        public const int LCD_DISPLAYMOVE = 0x08;
        public const int LCD_CURSORMOVE = 0x00;
        public const int LCD_MOVERIGHT = 0x04;
        public const int LCD_MOVELEFT = 0x00;
        public const int LCD_8BITMODE = 0x10;
        public const int LCD_4BITMODE = 0x00;
        public const int LCD_2LINE = 0x08;
        public const int LCD_1LINE = 0x00;
        public const int LCD_5x10DOTS = 0x04;
        public const int LCD_5x8DOTS = 0x00;
        public const int LCD_BACKLIGHT = 0x08;
        public const int LCD_NOBACKLIGHT = 0x00;
    }
}