﻿using Akka.Persistence;
using AkkaEventStore.Actors.Messages.Commands;
using AkkaEventStore.Messages.Commands;
using AkkaEventStore.Messages.Events;
using AkkaEventStore.Messages.Handlers.Basket;
using AkkaEventStore.Models;
using Newtonsoft.Json;
using System;

namespace AkkaEventStore.Actors
{
    public class BasketActorState : IActorState
    {
        public Basket basket = new Basket();

        public BasketActorState Update(IEvent evt)
        {
            return new BasketActorState { basket = BasketEventHandlers.Handle(basket, evt) };
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(basket, Formatting.Indented);
        }
    }

    public class BasketActor : PersistentStateActor
    {
        public override string PersistenceId { get; }

        public override IActorState State { get; set; }

        public BasketActor(string id)
        {
            State = new BasketActorState();
            (State as BasketActorState).basket.Id = id;
            PersistenceId = id;
        }

        public void UpdateState(IEvent evt)
        {
            State = (State as BasketActorState).Update(evt);
        }

        protected override bool ReceiveRecover(object message)
        {
            BasketActorState state;

            if (message is IEvent)
                UpdateState(message as IEvent);
            else if (message is SnapshotOffer && (state = ((SnapshotOffer)message).Snapshot as BasketActorState) != null)
                State = state;
            else if (message is RecoveryCompleted)
                Console.WriteLine($"{PersistenceId} Recovery Completed.");
            else
                return false;
            return true;
        }

        protected override bool ReceiveCommand(object message)
        {
            base.ReceiveCommand(message);

            if (message is CreateBasketCommand)
            {
                var cmd = (CreateBasketCommand)message;
                if (BasketCommandHandlers.Handle(State, cmd))                
                    Persist(new CreatedBasketEvent(cmd.basket), UpdateState);                
                else return false;
            }
            else if (message is AddLineItemToBasketCommand)
            {
                var cmd = (AddLineItemToBasketCommand)message;
                if (BasketCommandHandlers.Handle(State, cmd))
                    Persist(new AddedLineItemToBasketEvent(cmd.LineItem), UpdateState);
                else return false;
            }
            else if (message is RemoveLineItemFromBasketCommand)
            {
                var cmd = (RemoveLineItemFromBasketCommand)message;
                if (BasketCommandHandlers.Handle(State, cmd))
                    Persist(new RemovedLineItemFromBasketEvent(cmd.LineItem), UpdateState);
                else return false;
            }
            else return false;
            return true;
        }
    }
}
