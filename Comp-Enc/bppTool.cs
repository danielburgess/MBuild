class bppTool
{
    //2BPP Planar encoding to 1BPP-IL (Little Master 3)
    public static byte[] Convert2BPPto1BPPIL(byte[] ingfx)
    {
        byte[] outgfx = new byte[ingfx.Length];
        for (int c = 0; c < ingfx.Length; c += 16)
        {
            outgfx[c + 0] = ingfx[c + 0];
            outgfx[c + 1] = ingfx[c + 2];
            outgfx[c + 2] = ingfx[c + 4];
            outgfx[c + 3] = ingfx[c + 6];
            outgfx[c + 4] = ingfx[c + 8];
            outgfx[c + 5] = ingfx[c + 10];
            outgfx[c + 6] = ingfx[c + 12];
            outgfx[c + 7] = ingfx[c + 14];
            outgfx[c + 8] = ingfx[c + 1];
            outgfx[c + 9] = ingfx[c + 3];
            outgfx[c + 10] = ingfx[c + 5];
            outgfx[c + 11] = ingfx[c + 7];
            outgfx[c + 12] = ingfx[c + 9];
            outgfx[c + 13] = ingfx[c + 11];
            outgfx[c + 14] = ingfx[c + 13];
            outgfx[c + 15] = ingfx[c + 15];
        }
        return outgfx;
    }

    //1BPP-Interleaved to 2BPP Planar (Little Master 3)
    public static byte[] Convert1BPPILto2BPP(byte[] ingfx)
    {
        byte[] outgfx = new byte[ingfx.Length];
        for (int c = 0; c < ingfx.Length; c += 16)
        {
            outgfx[c + 0] = ingfx[c + 0];
            outgfx[c + 2] = ingfx[c + 1];
            outgfx[c + 4] = ingfx[c + 2];
            outgfx[c + 6] = ingfx[c + 3];
            outgfx[c + 8] = ingfx[c + 4];
            outgfx[c + 10] = ingfx[c + 5];
            outgfx[c + 12] = ingfx[c + 6];
            outgfx[c + 14] = ingfx[c + 7];
            outgfx[c + 1] = ingfx[c + 8];
            outgfx[c + 3] = ingfx[c + 9];
            outgfx[c + 5] = ingfx[c + 10];
            outgfx[c + 7] = ingfx[c + 11];
            outgfx[c + 9] = ingfx[c + 12];
            outgfx[c + 11] = ingfx[c + 13];
            outgfx[c + 13] = ingfx[c + 14];
            outgfx[c + 15] = ingfx[c + 15];
        }
        return outgfx;
    }
}

