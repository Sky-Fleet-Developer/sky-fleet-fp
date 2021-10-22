using System.Collections.Generic;

namespace Paterns.AbstractFactory
{

    public abstract class Generator<DefineT, ObjectT>
    {
        public abstract ObjectT Generate(DefineT define);

        public abstract bool CheckDefine(DefineT define);
    }

    public abstract class AbstractFactory<DefineT, ObjectT>
    {
        private List<Generator<DefineT, ObjectT>> generators;
        private Generator<DefineT, ObjectT> cashGenerator;

        public AbstractFactory()
        {
            generators = new List<Generator<DefineT, ObjectT>>();
            cashGenerator = null;
        }

        public void RegisterNewType(Generator<DefineT, ObjectT> generator)
        {
            generators.Add(generator);
        }

        public ObjectT Generate(DefineT define)
        {
            if(cashGenerator != null && cashGenerator.CheckDefine(define))
            {
                return cashGenerator.Generate(define);
            }
            for(int i = 0; i < generators.Count; i++)
            {
                if(generators[i].CheckDefine(define))
                {
                    cashGenerator = generators[i];
                    return generators[i].Generate(define);
                }
            }
            return GetDefault();
        }

        protected abstract ObjectT GetDefault();
    }
}
