﻿namespace T_Bot
{
    public class Pattern
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }

        public Pattern(string? name, string? path)
        {
            Name = name;
            Path = path;
        }
        public Pattern(int id, string? name, string? path):this( name, path)
        {
            Id = id;
        }
    }
}
