namespace CatalogAPI.Domain.Shared
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public Guid? CreatedBy { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }
        public Guid? UpdatedBy { get; protected set; }
        public bool IsActive { get; protected set; }

        protected Entity(Guid? createdBy = null)
        {
            if (createdBy.HasValue)
                EnsureResponsibleForChangeIsRequired(createdBy.Value);

            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy ?? Id;
            IsActive = true;
        }

        protected void MarkAsUpdated(Guid updatedBy)
        {
            EnsureResponsibleForChangeIsRequired(updatedBy);

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        protected void MarkAsActivated(Guid activatedBy)
        {
            IsActive = true;
            MarkAsUpdated(activatedBy);
        }

        protected void MarkAsDeactivated(Guid deactivatedBy)
        {
            IsActive = false;
            MarkAsUpdated(deactivatedBy);
        }

        private static void EnsureResponsibleForChangeIsRequired(Guid responsibleForChange)
        {
            if (responsibleForChange == Guid.Empty)
                throw new ArgumentException(DomainMessages.Entity.ResponsibleForChangeRequired);
        }
    }
}

