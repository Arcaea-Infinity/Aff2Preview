namespace AimuBotCS.Modules.Arcaea.Aff2Preview
{
    class NoteDesc
    {
        public int timing;
        public int divide = -1;
        public bool beyondFull = false;
        public bool hasDot = false;
        public bool isTriplet = false;

        static bool isDoubleEqual(double a, double b, double e) => Math.Abs(a - b) <= e;

        public NoteDesc(int timeing, int length, double bpm)
        {
            timing = timeing;
            double time_full_note = 60 * 1000 * 4 / bpm;
            if (length > time_full_note)
            {
                beyondFull = true;
                return;
            }

            for (int i = 1; i <= 32; i++)
            {
                var t_len = time_full_note / i;
                var t_dot_len = t_len * 1.5;

                if (isDoubleEqual(length, t_len, 2))
                {
                    divide = i;
                    return;
                }
                if (isDoubleEqual(length, t_dot_len, 2))
                {
                    divide = i;
                    hasDot = true;
                    return;
                }
            }

            for (int i = 1; i <= 32; i++)
            {
                var t_len = time_full_note / i;
                var t_triplet = t_len * 2 / 3;
                if (isDoubleEqual(length, t_triplet, 1))
                {
                    divide = i;
                    isTriplet = true;
                    return;
                }
            }
        }
    }
}
