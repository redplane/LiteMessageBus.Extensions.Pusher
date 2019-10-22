using System;

namespace LiteMessageBus.Extensions.PusherDemo.Models
{
    public class Item
    {
        #region Properties
        
        public Guid Id { get; }
        
        public string Name { get; }
        
        #endregion
        
        #region Constructor

        public Item(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion
        
        #region Methods

        public override string ToString()
        {
            return $"{Id} - {Name}";
        }

        #endregion
    }
}