using App.Domain.Game;
using App.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using MongoDB.Driver;

namespace App.Infrastructure.DomainRepository.Crud.GameParticipant
{
    public class Mongo : IGameParticipantRepository
    {
        private readonly IMongoCollection<Participant.Participant> _col;

        public Mongo(
            IMongoClient client,
            IConfiguration cfg)
        {
            var dbName = cfg["Mongo:DatabaseName"]
                         ?? throw new ArgumentNullException("Mongo:DatabaseName");
            var colName = cfg["Mongo:GameParticipantCollection"] ?? "gameParticipants";
            var db = client.GetDatabase(dbName);
            _col = db.GetCollection<Participant.Participant>(colName);
        }

        public Task<FSharpOption<Participant.Participant>> GetByIdAsync(Participant.Id id)
        {
            var filter = Builders<Participant.Participant>
                .Filter.Eq(p => p.Id.Item, id.Item);
            var findTask = _col.Find(filter).FirstOrDefaultAsync();

            var optTask = findTask.ContinueWith(t =>
                t.Result is not null
                    ? FSharpOption<Participant.Participant>.Some(t.Result)
                    : FSharpOption<Participant.Participant>.None
            );

            return optTask;
        }


        public Task SaveAsync(Participant.Id id, Participant.Participant participant)
        {
            var filter = Builders<Participant.Participant>
                .Filter.Eq(participant1 => participant1.Id.Item, id.Item);
            var options = new ReplaceOptions { IsUpsert = true };
            var upsert = _col.ReplaceOneAsync(filter, participant, options);

            upsert.ContinueWith(_ => default(Unit));
            return Task.CompletedTask;
        }

        public Task SaveAsync(FSharpList<Participant.Participant> participants)
        {
            var tasks = participants
                .Select(participant =>
                {
                    var filter = Builders<Participant.Participant>
                        .Filter.Eq(p => p.Id.Item, participant.Id.Item);
                    var options = new ReplaceOptions { IsUpsert = true };
                    return _col.ReplaceOneAsync(filter, participant, options);
                });

            Task.WhenAll(tasks).ContinueWith(_ => default(Unit));
            return Task.CompletedTask;
        }


        public Task RemoveAsync(Participant.Id id)
        {
            var filter = Builders<Participant.Participant>
                .Filter.Eq(p => p.Id.Item, id.Item);
            var deleteOp = _col.DeleteOneAsync(filter);

            deleteOp.ContinueWith(_ => default(Unit));
            return Task.CompletedTask;
        }
    }
}