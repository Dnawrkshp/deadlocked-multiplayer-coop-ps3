using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace DLMultiplayerCoop
{
    public partial class Form1 : Form
    {
        PS3TMAPI ps3 = new PS3TMAPI();

        public static uint ProcessID = 0;
        public bool isConAttached = false;
        bool isPlayback = false;
        public Timer kickTimer = new Timer();
        public Timer gameTimer = new Timer();
        IPEndPoint udp_sender;

        UdpClient udpC;


        public const uint UDP_ID_PLAYER =           0x00100010;
        public const uint UDP_ID_PLAYER_XZY =       0x00100001;
        public const uint UDP_ID_PLAYER_WEP =       0x00100011;
        public const uint UDP_ID_OBJECT_XZYROT =    0x00110001;
        public const uint UDP_ID_BOLTS =            0x00010001;
        public const uint UDP_ID_WEAPONS =          0x00010010;

        /* Input Box Argument Structure */
        public struct IBArg
        {
            public string label;
            public string defStr;
            public string retStr;
        };

        /* Brings up the Input Box with the arguments of a */
        public IBArg[] CallIBox(IBArg[] a)
        {
            InputBox ib = new InputBox();

            ib.Arg = a;
            ib.fmHeight = this.Height;
            ib.fmWidth = this.Width;
            ib.fmLeft = this.Left;
            ib.fmTop = this.Top;
            ib.TopMost = true;
            ib.fmForeColor = ForeColor;
            ib.fmBackColor = BackColor;
            ib.Show();

            while (ib.ret == 0)
            {
                a = ib.Arg;
                Application.DoEvents();
            }
            a = ib.Arg;

            if (ib.ret == 1)
                return a;
            else if (ib.ret == 2)
                return null;

            return null;
        }

        public Form1()
        {
            InitializeComponent();

            this.FormClosing += (sender, e) =>
            {
                if (isConAttached)
                    PS3TMAPI.StopPadPlayback(0);
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PS3TMAPI.InitTargetComms();
            uint numtar;
            PS3TMAPI.GetNumTargets(out numtar);
            if (numtar <= 0)
            {
                return;
            }
            //PS3TMAPI.PickTarget(
            if (PS3TMAPI.Connect(0, null) != PS3TMAPI.SNRESULT.SN_E_COMMS_ERR)
                Text = "Connected";
            else
                Text = "Failed to connect";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PS3TMAPI.SNRESULT snr;
            uint[] pIDs;
            PS3TMAPI.GetProcessList(0, out pIDs);
            if (pIDs != null && pIDs.Length > 0)
            {
                ProcessID = pIDs[0];
                if (PS3TMAPI.ProcessAttach(0, PS3TMAPI.UnitType.PPU, ProcessID) == PS3TMAPI.SNRESULT.SN_S_OK)
                {
                    Text = "Connected and Attached";
                    PS3TMAPI.ProcessContinue(0, ProcessID);
                    isConAttached = true;

                   

                    if (SetupUDP())
                    {
                        //byte[] origPad = Extension.ReadBytes((ulong)Extension.ReadUInt32(0x010DA570), 0x800);
                        //Extension.WriteBytes(pAddr, origPad);
                        //Extension.WriteUInt32(0x010DA570, pAddr);

                        byte[] Switch_Player_Weapon_RPC = { 0xF8, 0x21, 0xFF, 0xD1, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x01, 0x00, 0x08, 0x38, 0x60, 0xCF, 0x00, 0x54, 0x63, 0x80, 0x1E, 0x80, 0x83, 0x01, 0x24, 0x2C, 0x04, 0x00, 0x01, 0x40, 0x82, 0x00, 0x20, 0x38, 0x80, 0x00, 0x00, 0x90, 0x83, 0x01, 0x24, 0x80, 0x83, 0x01, 0x28, 0x38, 0x60, 0x00, 0x01, 0x4A, 0xB6, 0xB4, 0xDD, 0x60, 0x00, 0x00, 0x00, 0x60, 0x00, 0x00, 0x00, 0xE8, 0x01, 0x00, 0x08, 0x7C, 0x08, 0x03, 0xA6, 0x38, 0x21, 0x00, 0x30, 0x4E, 0x80, 0x00, 0x20 };
                        byte[] Switch_Player_Weapon_RPC_hook = { 0x49, 0x61, 0x8E, 0x40 };
                        Extension.WriteBytes(0xCF000124, new byte[8]);
                        Extension.WriteBytes(0x017F4F00, Switch_Player_Weapon_RPC);
                        Extension.WriteBytes(0x001DC0C0, Switch_Player_Weapon_RPC_hook);

                        byte[] PadPatchetCondensed2 = { 0xF8, 0x21, 0xFF, 0x91, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x01, 0x00, 0x08, 0xFB, 0xE1, 0x00, 0x30, 0xFB, 0xC1, 0x00, 0x38, 0x2C, 0x03, 0x00, 0x01, 0x40, 0x82, 0x00, 0x28, 0x60, 0x00, 0x00, 0x00, 0x4B, 0x09, 0x3E, 0xF5, 0x3B, 0xE0, 0xCF, 0x00, 0x57, 0xFF, 0x80, 0x1E, 0xEB, 0xDF, 0x12, 0x00, 0xFB, 0xC4, 0x00, 0x04, 0xEB, 0xDF, 0x12, 0x08, 0xFB, 0xC4, 0x00, 0x0C, 0x48, 0x00, 0x00, 0x08, 0x4B, 0x09, 0x3E, 0xD5, 0xE8, 0x01, 0x00, 0x08, 0x7C, 0x08, 0x03, 0xA6, 0xEB, 0xE1, 0x00, 0x30, 0xEB, 0xC1, 0x00, 0x38, 0x38, 0x21, 0x00, 0x70, 0x4E, 0x80, 0x00, 0x20 }; 
                        byte[] PadPatchetCondensed2_hook = { 0x49, 0x0E, 0xD1, 0x01 };
                        Extension.WriteBytes(0x017F4180, PadPatchetCondensed2);
                        Extension.WriteBytes(0x00707080, PadPatchetCondensed2_hook);


                        if (kickTimer != null)
                            kickTimer.Stop();
                        kickTimer = new Timer();
                        kickTimer.Interval = 10;
                        kickTimer.Tick += kickTimer_Tick;
                        kickTimer.Start();

                        if (gameTimer != null)
                            gameTimer.Stop();
                        gameTimer = new Timer();
                        gameTimer.Interval = 10000;
                        gameTimer.Tick += gameTimer_Tick;
                        gameTimer.Start();
                    }
                    else
                    {
                        MessageBox.Show("Failed to setup UDP");
                    }


                }
                else
                    Text = "Failed to attach";
            }
            else
                Text = "No game started";
        }

        bool SetupUDP()
        {
            if (checkBox1.Checked) //is host
            {
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 10064);
                udpC = new UdpClient(ipep);

                udp_sender = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = udpC.Receive(ref udp_sender);
                MessageBox.Show("Client connected!");
                udpC.BeginReceive(new AsyncCallback(recv_callback), null);
                return true;
            }
            else
            {
                udpC = new UdpClient();

                IBArg[] a = new IBArg[1];
                a[0].defStr = "0.0.0.0";
                a[0].label = "Enter the host IP address";

                a = CallIBox(a);

                if (a != null && a[0].retStr != null)
                {
                    udpC.Connect(a[0].retStr, 10064);
                    udpC.Send(new byte[400], 400); //, new IPEndPoint(IPAddress.Parse(a[0].retStr), 10064));
                }

                if (udpC.Client.Connected)
                {
                    udpC.BeginReceive(new AsyncCallback(recv_callback), null);
                    return true;
                }
            }

            return false;
        }

        private void recv_callback(IAsyncResult res)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
            byte[] received = udpC.EndReceive(res, ref RemoteIpEndPoint);
            udpC.BeginReceive(new AsyncCallback(recv_callback), null);

            //Process codes
            //Console.WriteLine(received.Length);

            byte[] baID = new byte[4];
            Array.Copy(received, 0, baID, 0, 4);
            uint id = BitConverter.ToUInt32(baID, 0);

            if (id == UDP_ID_PLAYER)
            {
                byte[] pad = new byte[received.Length - 4 - 4 - 4 - 12 - 1 - 4];
                Array.Copy(received, 4, pad, 0, pad.Length);

                byte[] health = new byte[4];
                Array.Copy(received, pad.Length + 4, health, 0, health.Length);

                byte[] lr = new byte[4];
                Array.Copy(received, pad.Length + health.Length + 4, lr, 0, lr.Length);

                byte[] ud = new byte[4];
                Array.Copy(received, pad.Length + health.Length + lr.Length + 4, ud, 0, ud.Length);

                byte[] xzy = new byte[12];
                Array.Copy(received, pad.Length + health.Length + lr.Length + ud.Length + 4, xzy, 0, xzy.Length);

                Invoke((MethodInvoker)delegate
                {
                    Form1_DoUpdateP2(pad, health, lr, ud, xzy);
                });
            }
            else if (id == UDP_ID_BOLTS)
            {
                byte[] bolts = new byte[4];
                Array.Copy(received, 4, bolts, 0, bolts.Length);

                Invoke((MethodInvoker)delegate
                {
                    Form1_DoUpdate_GameBolts(bolts);
                });
            }
            else if (id == UDP_ID_PLAYER_XZY)
            {
                byte[] xzy = new byte[12];
                Array.Copy(received, 4, xzy, 0, xzy.Length);

                Invoke((MethodInvoker)delegate
                {
                    Form1_DoUpdate_PlayerXZY(xzy);
                });
            }
            else if (id == UDP_ID_PLAYER_WEP)
            {
                byte wep = received[received.Length - 1];
                Invoke((MethodInvoker)delegate
                {
                    Form1_DoUpdateP2_Wep(wep);
                });
            }
            else if (id == UDP_ID_OBJECT_XZYROT)
            {
                byte[] objXZY = new byte[12];
                byte[] objROT = new byte[12];
                byte[] objOffBA = new byte[4];
                byte[] objIDBA = new byte[2];
                Array.Copy(received, 4, objOffBA, 0, objOffBA.Length);
                uint objOff = BitConverter.ToUInt32(objOffBA, 0);
                Array.Copy(received, 8, objXZY, 0, objXZY.Length);
                Array.Copy(received, 20, objROT, 0, objROT.Length);
                Array.Copy(received, 32, objIDBA, 0, objIDBA.Length);
                ushort objID = BitConverter.ToUInt16(objIDBA, 0);
                Invoke((MethodInvoker)delegate
                {
                    Form1_DoUpdateOBJ_XZYROT(objOff, objID, objXZY, objROT);
                });
            }
            else if (id == UDP_ID_WEAPONS)
            {
                Invoke((MethodInvoker)delegate
                {
                    Form1_DoUpdate_Weapons(received);
                });
            }


            //MessageBox.Show(Encoding.UTF8.GetString(received));
        }

        bool isBAEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            for (int x = 0; x < a.Length; x++)
            {
                if (a[x] != b[x])
                    return false;
            }
            return true;
        }

        bool isPassenger(int pID)
        {
            uint ptr, val = Extension.ReadUInt32(0x00ACED00 + (uint)(pID * 4));
            ushort id;


            switch (pID)
            {
                case 0:
                    ptr = Extension.ReadUInt32(0x00ACF184);
                    id = Extension.ReadUInt16(ptr + 0xBC);

                    if (val == 0 || id == 0x2054)
                        return false;
                    break;
                case 1:
                    ptr = Extension.ReadUInt32(0x00ACF5F4);
                    id = Extension.ReadUInt16(ptr + 0xBC);

                    if (val == 0 || id == 0x2054)
                        return false;
                    break;
            }

            return true;
        }

        void Form1_DoUpdate_Weapons(byte[] recv)
        {
            int off = 4, ind = 1;
            uint ptr = Extension.ReadUInt32(0x010D6B14);

            ulong memOff = ptr + 0xA74 + 0x44;
            byte[] wepInfo = new byte[recv.Length - 4];
            Array.Copy(recv, 4, wepInfo, 0, wepInfo.Length);
            Extension.WriteBytes(memOff, wepInfo);

            return;
            while (off < recv.Length)
            {
                byte[] t = new byte[2];
                short wepLvl, curWepLvl, wepAmmo, curWepAmmo;
                int wepExp, curWepExp;
                byte weaponMod, curWepMod;

                memOff = ptr + 0xA74 + (uint)(0x44 * ind);

                byte[] block = new byte[(recv.Length - 4) / 0x10];
                Array.Copy(recv, 4 + (ind * block.Length), block, 0, block.Length);

                Extension.WriteBytes(memOff, block);

                //Array.Copy(recv, off, t, 0, 2);
                //Array.Reverse(t);
                //wepLvl = BitConverter.ToInt16(t, 0);

                //t = new byte[2];
                //Array.Copy(recv, off + 2, t, 0, 2);
                //Array.Reverse(t);
                //wepAmmo = BitConverter.ToInt16(t, 0);
                //t = new byte[4];
                //Array.Copy(recv, off + 4, t, 0, 4);
                //Array.Reverse(t);
                //wepExp = BitConverter.ToInt32(t, 0);
                //weaponMod = recv[off + 0xC];

                //if (wepAmmo > 300 || wepAmmo < -1)
                //{

                //}

                //curWepLvl = Extension.ReadInt16(memOff);
                //curWepAmmo = Extension.ReadInt16(memOff + 2);
                //curWepExp = Extension.ReadInt32(memOff + 4);
                //curWepMod = Extension.ReadByte(memOff + 0xC);

                //if (curWepAmmo != wepAmmo)
                //    Extension.WriteUInt16(memOff + 2, (ushort)wepAmmo);
                //if (curWepExp < wepExp || curWepLvl < wepLvl)
                //    Extension.WriteUInt32(memOff + 4, (uint)wepExp);
                //if (curWepLvl < wepLvl)
                //    Extension.WriteUInt16(memOff, (ushort)wepLvl);
                //if (weaponMod != curWepMod)
                //    Extension.WriteByte(memOff + 0xC, weaponMod);

                off += 0x10; ind++;
            }
        }

        void Form1_DoUpdateOBJ_XZYROT(uint addr, ushort id, byte[] xzy, byte[] rot)
        {
            if (Extension.ReadUInt16(addr + 0xBC) == id)
            {
                Extension.WriteBytes(addr + 0x10, xzy);
                Extension.WriteBytes(addr + 0xF0, rot);
            }
        }

        byte[] oldXZY2;
        uint vehiclePtr;
        bool isP2Passenger;
        void Form1_DoUpdate_PlayerXZY(byte[] xzy)
        {
            if (!isBAEqual(xzy, oldXZY2))
            {
                vehiclePtr = Extension.ReadUInt32(0x00ACF5F4);
                isP2Passenger = isPassenger(1);
                if (vehiclePtr == 0 || isP2Passenger)
                    Extension.WriteBytes(0x010D7710, xzy);
                else
                    Extension.WriteBytes(vehiclePtr + 0x10, xzy);
                oldXZY2 = xzy;
            }
        }

        byte[] oldBolts;
        void Form1_DoUpdate_GameBolts(byte[] bolts)
        {
            //Extension.WriteUInt32(0x010DA570, pAddr);
            if (!isBAEqual(bolts, oldBolts))
            {
                Extension.WriteBytes(0x009C32E8, bolts);
                oldBolts = bolts;
            }
        }

        uint pAddr = 0x00B00800;
        byte[] oldHealth, oldLR, oldUD, oldXZY;
        byte oldWep;
        void Form1_DoUpdateP2_Wep(byte wep)
        {
            if (wep != 0)
                oldWep = wep;
        }
        
        byte[] oldPad;
        void Form1_DoUpdateP2(byte[] pad, byte[] health, byte[] lr, byte[] ud, byte[] xzy)
        {
            //Extension.WriteUInt32(0x010DA570, pAddr);
            if (!isBAEqual(pad, oldPad))
            {
                Extension.WriteBytes(0xCF001200, pad);
                oldPad = pad;
            }

            if (!isBAEqual(health, oldHealth))
            {
                Extension.WriteBytes(0x010DA490, health);
                oldHealth = health;
            }

            if (!isBAEqual(lr, oldLR))
            {
                oldLR = lr;
                if (vehiclePtr == 0)
                    Extension.WriteBytes(0x010D9020, lr);
                else
                {
                    if (isP2Passenger)
                    {
                        uint ptr = Extension.ReadUInt32(vehiclePtr + 0xAC);
                        Extension.WriteBytes(ptr + 0x1B0, lr);
                    }
                    else
                    {
                        Extension.WriteBytes(vehiclePtr + 0xF8, lr);
                        uint ptr = Extension.ReadUInt32(vehiclePtr + 0xAC);
                        Extension.WriteBytes(ptr + 0x1B0, lr);
                    }

                    Array.Reverse(lr);
                    float lrflt = BitConverter.ToSingle(lr, 0);
                    lrflt -= (float)(Math.PI / 2);
                    //Extension.WriteFloat(0x010D9020, lrflt);
                }
            }

            if (!isBAEqual(ud, oldUD))
            {
                if (vehiclePtr == 0)
                    Extension.WriteBytes(0x010D9040, ud);
                else
                {
                    uint ptr = Extension.ReadUInt32(vehiclePtr + 0xAC);
                    Extension.WriteBytes(ptr + 0x1D0, ud);
                }
                oldUD = ud;
            }

            if (!isBAEqual(xzy, oldXZY))
            {
                Extension.WriteBytes(0x010DA574, xzy);
                oldXZY = xzy;
            }
        }

        void gameTimer_Tick(object sender, EventArgs e)
        {
            /*
                //Speed hack (counteracts existing lag from coop)
                2 009C28F4 0.023
                //Screens to 1
                0 00B36DF0 00000000
                /0 00B36DF8 00000000
             */

            //Write speed hack (counteract existing coop frame lag)
            //Extension.WriteFloat(0x009C28F4, 0.023f);

            //Update game stats (weapons, ammo, bolts, etc)
            byte[] packet, id;
            
            if (checkBox1.Checked)
            {
                //Send weapon info
                
                uint wepPtr = Extension.ReadUInt32(0x010D6B14);
                byte[] weaponInfo = Extension.ReadBytes(wepPtr + 0xA74 + 0x44, 0x44 * 0x10);
                packet = new byte[4 + weaponInfo.Length];
                id = BitConverter.GetBytes(UDP_ID_WEAPONS);
                id.CopyTo(packet, 0);
                weaponInfo.CopyTo(packet, 4);
                Send(packet);

                byte[] bolts = Extension.ReadBytes(0x009C32E8, 4);
                id = BitConverter.GetBytes(UDP_ID_BOLTS);

                packet = new byte[4 + bolts.Length];
                id.CopyTo(packet, 0);
                bolts.CopyTo(packet, 4);
                Send(packet);

                /*
                byte[] xzy = Extension.ReadBytes(0x010D44D0, 12);
                id = BitConverter.GetBytes(UDP_ID_PLAYER_XZY);

                packet = new byte[4 + xzy.Length];
                id.CopyTo(packet, 0);
                xzy.CopyTo(packet, 4);
                Send(packet);
                */

                //Update game objects (loc and rot) if host
                uint objPtr = Extension.ReadUInt32(0x00ACFA60);
                ushort objID;
                //List of objs that should be updated (vehicles)
                ushort[] objs = new ushort[]
                {
                    0x2064,
                    0x2054,
                    0x2038,
                    0x2039,
                    0x205e,
                    0x20ae,
                    0x213c
                };
                while ((objID = Extension.ReadUInt16(objPtr + 0xBC)) != 0)
                {
                    if (objs.Contains(objID) && Extension.ReadUInt32(0x00ACF184) != objPtr && Extension.ReadUInt32(0x00ACF5F4) != objPtr)
                    {
                        byte[] objXZY = Extension.ReadBytes(objPtr + 0x10, 12);
                        byte[] objROT = Extension.ReadBytes(objPtr + 0xF0, 12);
                        byte[] objOff = BitConverter.GetBytes(objPtr);
                        byte[] objIDBA = BitConverter.GetBytes(objID);
                        id = BitConverter.GetBytes(UDP_ID_OBJECT_XZYROT);

                        packet = new byte[4 + objXZY.Length + objROT.Length + objOff.Length + objIDBA.Length];
                        id.CopyTo(packet, 0);
                        objOff.CopyTo(packet, 4);
                        objXZY.CopyTo(packet, 8);
                        objROT.CopyTo(packet, 20);
                        objIDBA.CopyTo(packet, 32);
                        Send(packet);
                    }

                    objPtr += 0x100;
                    //Console.WriteLine(objPtr.ToString("X8"));
                }
                
            }

            //throw new NotImplementedException();
        }

        int delayCount = 0;
        void kickTimer_Tick(object sender, EventArgs e)
        {
            if (Extension.ReadByte(0x00B1F4EF) != oldWep && delayCount == 0) // && Extension.ReadByte(0x010D998B) != oldWep)
            {
                //for (int llll = 0; llll < 2; llll++)
                {
                    //Write p2 wep
                    //Extension.WriteByte(0x010D9CFF, oldWep);
                    //Extension.WriteByte(0x00B1F4EF, oldWep);
                    //Extension.WriteByte(0x00B1F4F3, oldWep);
                    //Extension.WriteByte(0x00B1F4F7, oldWep);
                    //Extension.WriteByte(0x010D9B23, oldWep);
                    //Extension.WriteByte(0x010D998B, oldWep);
                    //Extension.WriteByte(0x010D7E6F, oldWep);

                    //Write switch wep
                    Extension.WriteUInt32(0xCF000128, (uint)oldWep);
                    Extension.WriteUInt32(0xCF000124, 1);
                }
                //Extension.WriteByte(0x010D9CFF, oldWep);
                //Extension.WriteUInt32(pAddr + 0xA4, 0x00000010);

                delayCount = 0;
            }
            

            if (Extension.ReadByte(0x0110D42B) != 0)
            {
                return;
            }

            //Write p2 wep
            //Extension.WriteByte(0x010D9CFF, oldWep);
            //Extension.WriteByte(0x010D9B23, oldWep);
            //Extension.WriteByte(0x010D998B, oldWep);
            //Extension.WriteByte(0x010D7E6F, oldWep);

            uint _vehiclePtr = Extension.ReadUInt32(0x00ACF184);
            uint ptr = Extension.ReadUInt32(vehiclePtr + 0xAC);
            bool passenger = isPassenger(0);

            //Read pad, health, orientation, equipped weapon -- P0
            byte[] lr, ud;
            if (_vehiclePtr == 0)
                lr = Extension.ReadBytes(0x010D5DE0, 4);
            else
            {
                if (passenger)
                    lr = Extension.ReadBytes(ptr + 0x1B0, 4);
                else
                    lr = Extension.ReadBytes(_vehiclePtr + 0xF8, 4);
            }
            if (_vehiclePtr == 0 || !passenger)
                ud = Extension.ReadBytes(0x010D5E00, 4);
            else
                ud = Extension.ReadBytes(ptr + 0x1D0, 4);
            byte[] xzy = Extension.ReadBytes(0x010D7334, 12);
            byte[] health = Extension.ReadBytes(0x010D7250, 4);

            byte[] pad = Extension.ReadBytes(0x010D7410, 0x10);
            byte[] id = BitConverter.GetBytes(UDP_ID_PLAYER);

            byte[] packet = new byte[lr.Length + ud.Length + health.Length + pad.Length + xzy.Length + 1 + 4];
            id.CopyTo(packet, 0);
            pad.CopyTo(packet, 4);
            health.CopyTo(packet, pad.Length + 4);
            lr.CopyTo(packet, pad.Length + health.Length + 4);
            ud.CopyTo(packet, pad.Length + health.Length + lr.Length + 4);
            xzy.CopyTo(packet, pad.Length + health.Length + lr.Length + ud.Length + 4);
            Send(packet);

            packet = new byte[5];
            byte wep = Extension.ReadByte(0x010D674B);
            new byte[] { wep }.CopyTo(packet, 4);
            id = BitConverter.GetBytes(UDP_ID_PLAYER_WEP);
            id.CopyTo(packet, 0);
            Send(packet);

            if (delayCount == 0)
            {
                if (_vehiclePtr == 0 || passenger)
                    xzy = Extension.ReadBytes(0x010D44D0, 12);
                else
                    xzy = Extension.ReadBytes(_vehiclePtr + 0x10, 12);
                id = BitConverter.GetBytes(UDP_ID_PLAYER_XZY);
            
                packet = new byte[4 + xzy.Length];
                id.CopyTo(packet, 0);
                xzy.CopyTo(packet, 4);
                Send(packet);

                //Write 1 screen hack
                Extension.WriteByte(0x00B36DF3, 0);

                //Check if start is not open
                if (!Extension.ReadBool(0x0110D43F))
                {
                    //Write start index to 0
                    Extension.WriteUInt32(0x0119F9A8, 0);

                    //Write challenge index to 0
                    Extension.WriteUInt32(0x011A0AE0, 0);
                }

                delayCount = 10;
            }

            if (delayCount > 0)
                delayCount--;
        }

        void Send(byte[] packet)
        {
            if (!checkBox1.Checked && udpC.Client.Connected)
            {
                udpC.Send(packet, packet.Length);
            }
            else // if (udpC.Client.Connected)
            {
                //udp_sender.Port = 10064;
                udpC.Send(packet, packet.Length, udp_sender);
            }
        }





        void PressButtonDL(string buttonArgs)
        {
            ulong addr = pAddr;

            //ulong addr = 0x00B11E80;
            string[] buttons = buttonArgs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            uint val = 0;
            int[] offsets = new int[] { 0xA0, 0xAC, 0xB0, 0xBC, 0xC0, 0x100, 0x104, 0x108, 0x10C, 0x110, 0x114, 0x118, 0x11C, 0x120, 0x124, 0x128, 0x12C, 0x130, 0x154, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270, 0x478, 0x4E4 };
            byte[] orig = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3E, 0xF8, 0xF8, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3E, 0xF8, 0xF8, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3E, 0xF8, 0xF8, 0xFA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x99, 0x0D, 0x00, 0x01, 0x0D, 0x74, 0x10, 0x00, 0x00, 0x00, 0x7C, 0x00, 0x00, 0x00, 0x08, 0x00, 0x7F, 0x00, 0x7E, 0x00, 0x7F, 0x00, 0x7B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x7C, 0x00, 0x00, 0x00, 0x08, 0x00, 0x7F, 0x00, 0x7E, 0x00, 0x7F, 0x00, 0x7B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            List<int[]> offs = new List<int[]>();
            offs.Add(offsets);

            orig = ParseOffsets(orig, offs, val, 1);
            offs.Clear();

            orig = ParseSwitchButtons(orig, buttons);

            //orig = ParseOffsets(orig, offs, val);

            //if (usingorig)
            //{
            //    for (int zzz = 0; zzz < 10; zzz++)
            //        NCInterface.SetMemory(0x00B11E80, orig);
            //}

            //Read l/r and u/d rotations into orig
            byte[] rot = Extension.ReadBytes(addr, 8);
            Array.Copy(rot, 0, orig, 0, 8);

            Extension.WriteBytes(addr, orig);
        }

        byte[] ParseSwitchButtons(byte[] orig, string[] buttons)
        {
            int[] offsets;
            uint val = 0;

            for (int x = 0; x < buttons.Length; x++)
            {
                switch (buttons[x].ToUpper())
                {
                    case "X":
                        val |= 0x00000040;
                        offsets = new int[] { 0xA0, 0xA4, 0xCC, 0xC0, 0xC4, 0x10C, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000040);
                        break;
                    case "SQR":
                        val |= 0x00000080;
                        offsets = new int[] { 0xA4, 0xC0, 0xC4, 0x11C, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000080);
                        break;
                    case "TRI":
                        val |= 0x00000010;
                        offsets = new int[] { 0xA4, 0xC0, 0xC4, 0xA0, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000010);
                        break;
                    case "O":
                        val |= 0x00000020;
                        offsets = new int[] { 0xA4, 0xC0, 0xC4, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000020);
                        break;
                    case "STRT":
                        val |= 0x00080000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00080000);

                        List<int[]> start = new List<int[]>();
                        start.Add(new int[] { 0x264, 0x270, 0x248, 0xE4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, start, 0x00000800);
                        break;
                    case "SEL":
                        val |= 0x00010000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00010000);

                        List<int[]> sel = new List<int[]>();
                        sel.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, sel, 0x00000100);
                        break;
                    case "L3":
                        val |= 0x00020000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00020000);

                        List<int[]> l3 = new List<int[]>();
                        l3.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, l3, 0x00000200);
                        break;
                    case "R3":
                        val |= 0x00040000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00040000);

                        List<int[]> r3 = new List<int[]>();
                        r3.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, r3, 0x00000400);
                        break;
                    case "L2":
                        val |= 0x00000001;
                        offsets = new int[] { 0xA0, 0xC0, 0xC4, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000001);
                        break;
                    case "L2_2": //Double tap
                        val |= 0x00000001;
                        offsets = new int[] { 0xA4, 0xC0, 0xC4, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000001);
                        break;
                    case "R2":
                        val |= 0x00000002;
                        offsets = new int[] { 0xA4, 0xA0, 0xC0, 0xC4, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000002);
                        break;
                    case "L1":
                        val |= 0x00000004;
                        offsets = new int[] { 0xA4, 0x10C, 0xC0, 0xC4, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000004);
                        break;
                    case "R1":
                        val |= 0x00000008;
                        offsets = new int[] { 0xA0, 0xA4, 0xC4, 0xC0, 0x248, 0x254, 0x258, 0x25C, 0x260, 0x264, 0x268, 0x26C, 0x270 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00000008);
                        break;
                    case "UP":
                        val |= 0x00100000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00100000);

                        List<int[]> up = new List<int[]>();
                        up.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, up, 0x00001000);
                        break;
                    case "DOWN":
                        val |= 0x00400000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00400000);

                        List<int[]> down = new List<int[]>();
                        down.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, down, 0x00004000);
                        break;
                    case "LEFT":
                        val |= 0x00800000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00800000);

                        List<int[]> left = new List<int[]>();
                        left.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, left, 0x00008000);
                        break;
                    case "RIGHT":
                        val |= 0x00200000;
                        offsets = new int[] { 0x478, 0x4E4 };
                        orig = ParseOffsets(orig, new List<int[]>() { offsets }, 0x00200000);

                        List<int[]> right = new List<int[]>();
                        right.Add(new int[] { 0x264, 0x270, 0x248, 0xA4, 0xC4, 0xC0 });
                        orig = ParseOffsets(orig, right, 0x00002000);
                        break;
                    case "FWD":
                        orig = ParseOffsets(orig, new List<int[]>() { new int[] { 0x0C } }, 0xBF800000, 1);
                        break;
                    case "BWD":
                        orig = ParseOffsets(orig, new List<int[]>() { new int[] { 0x0C } }, 0x3F800000, 1);
                        break;
                    case "SLFT":
                        orig = ParseOffsets(orig, new List<int[]>() { new int[] { 0x08 } }, 0xBF800000, 1);
                        break;
                    case "SRGT":
                        orig = ParseOffsets(orig, new List<int[]>() { new int[] { 0x08 } }, 0x3F800000, 1);
                        break;
                }
            }
            return orig;
        }

        byte[] ParseOffsets(byte[] orig, List<int[]> offs, uint val, int mode = 0)
        {
            byte[] valBA = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(valBA);

            for (int z = 0; z < offs.Count; z++)
            {
                for (int zz = 0; zz < offs[z].Length; zz++)
                {
                    switch (mode)
                    {
                        case 0:
                            orig[offs[z][zz] + 0] |= valBA[0];
                            orig[offs[z][zz] + 1] |= valBA[1];
                            orig[offs[z][zz] + 2] |= valBA[2];
                            orig[offs[z][zz] + 3] |= valBA[3];
                            break;
                        case 1:
                            orig[offs[z][zz] + 0] = valBA[0];
                            orig[offs[z][zz] + 1] = valBA[1];
                            orig[offs[z][zz] + 2] = valBA[2];
                            orig[offs[z][zz] + 3] = valBA[3];
                            break;
                    }
                }
            }

            return orig;
        }
    }
}
