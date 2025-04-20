namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class UpdateKey
    {
        public UpdateType UpdateType { get; }

        public int Id { get; }

        public UpdateKey(UpdateType updateType, int id)
        {
            UpdateType = updateType;
            Id = id;
        }

        public override bool Equals(object? obj)
        {
            if (obj is UpdateKey other)
            {
                return UpdateType == other.UpdateType && Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UpdateType, Id);
        }

        public override string ToString()
        {
            return $"{UpdateType}_{Id}";
        }
    }
}