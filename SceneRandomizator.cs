using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneRandomizator2 : MonoBehaviour
{

    public List<GameObject> rooms;
    public GameObject wallPrefab;
    public Vector3 wallOffset;

    //public uint roomKind; 

    //todo
    //[Header("1 Way Rooms")]
    //[Tooltip("Il faut que le total des proba = 100 sinon ca ne marche pas!!!")]
    //public List<GameObject> oneWayRooms;
    //public float oneWaySpawnProbability;
    //
    //[Header("2 Way Rooms")]
    //public List<GameObject> twoWayRooms;
    //public float twoWaySpawnProbability;
    //
    //[Header("3 Way Rooms")]
    //public List<GameObject> threeWayRooms;
    //public float threeWaySpawnProbability;
    //
    //[Header("4 Way Rooms")]
    //public List<GameObject> fourWayRooms;
    //public float fourWaySpawnProbability;
    // fin todo




    [Space(20)]
    public Vector2Int minMaxRoom;
    public float checkDistanceForOtherRoom;

    [Header("Serialized fields")]
    [SerializeField] private List<GameObject> instantiatedRooms;
    [SerializeField] private Vector3 sphereGizmosOrigin;
    [SerializeField] private int roomsLeftToGenerate;
    [SerializeField] private bool isLevelGenerationFinished;

    private void Start()
    {
        isLevelGenerationFinished = false;
        roomsLeftToGenerate = Random.Range(minMaxRoom.x, minMaxRoom.y + 1);
        //if (!ValidateProbabilities()) Debug.LogWarning("Total des proba pas egales a 100. Peut causer des problemes de generation");
        StartCoroutine(GeneralCoroutine());
    }

    private IEnumerator Generate(GameObject lastRoomGenerated)
    {
        if (roomsLeftToGenerate <= 0)
        {
            Debug.Log("Plus de rooms a generer, quittation");
            StartCoroutine(FillInWalls(instantiatedRooms));
            yield break;
        }

        List<int> checkedEntrancesIndex = new List<int>();
        //Debug.Log(roomsLeftToGenerate + " salles restantes a generer.");

        int randomEntranceNumber = Random.Range(0, lastRoomGenerated.GetComponent<RoomInfoScript>().Entrances.Count);
        GameObject randomEntrance = lastRoomGenerated.GetComponent<RoomInfoScript>().Entrances[randomEntranceNumber];

        //Ajouter cette entrance a checkedEntranceIndex:
        checkedEntrancesIndex.Add(randomEntranceNumber);

        int randomRoomToGenerate = Random.Range(0, rooms.Count);

        GameObject roomToGenerate = rooms[randomRoomToGenerate];

        //Si l'endroit ou on va spawn une room contient deja une room, on cherche une autre entrance:
        while (IsFacingAnotherRoom(lastRoomGenerated, randomEntrance)
            && (checkedEntrancesIndex.Count != lastRoomGenerated.GetComponent<RoomInfoScript>().Entrances.Count))
        {
            randomEntranceNumber = Random.Range(0, lastRoomGenerated.GetComponent<RoomInfoScript>().Entrances.Count);
            randomEntrance = lastRoomGenerated.GetComponent<RoomInfoScript>().Entrances[randomEntranceNumber];

            //Si l'entrance n'a jamais ete evalué, on l'ajoute a la liste des entrances deja evalues
            if (!checkedEntrancesIndex.Contains(randomEntranceNumber))
            {
                checkedEntrancesIndex.Add(randomEntranceNumber);
            }

            //True si toutes les entrances ont ete evaluees et aucune n'est libre
            if(checkedEntrancesIndex.Count == lastRoomGenerated.GetComponent<RoomInfoScript>().Entrances.Count
                && IsFacingAnotherRoom(lastRoomGenerated, randomEntrance)) {
                //Debug.Log("Tout les entrances checked pour la room: " + lastRoomGenerated + "//// Backtracking...");
                yield break;
            }
        }

        //=========== FOR THE NEW ROOM TO BE CREATED ============= //

        int randomNewRoomEntranceNumber = Random.Range(0, roomToGenerate.GetComponent<RoomInfoScript>().Entrances.Count);
        GameObject randomNewRoomEntrance = roomToGenerate.GetComponent<RoomInfoScript>().Entrances[randomNewRoomEntranceNumber];

        //newPosition and newRotation for the new room to be instantiated:
        //Offset calculated between a random entrance in the new room and the the origin of the room (which is 0,0,0)
        Vector3 offset = randomNewRoomEntrance.transform.localPosition;
        //newPos = pos of the random entrance chosen for the current room + offset of random entrance of the new room
        Vector3 newPosition = randomEntrance.transform.position + randomEntrance.transform.right * offset.magnitude;
        //Rotate la room a generer de sorte a ce qu'elle fasse face a la room deja generee:
        Quaternion newRotation = RotateQuaternion(randomEntrance.transform.rotation, 0, 180, 0);

        

        //Instantiation de la nouvelle salle:
        lastRoomGenerated = Instantiate(roomToGenerate, newPosition, newRotation);
        lastRoomGenerated.name = "Salle " + roomsLeftToGenerate;
        instantiatedRooms.Add(lastRoomGenerated);
        roomsLeftToGenerate--;

        yield return new WaitForSeconds(.7f);
        yield return StartCoroutine(Generate(lastRoomGenerated));

        //Backtracking:
        //S'il ne reste plus de rooms a generer:
        if(roomsLeftToGenerate <= 0)
        {
            //Debug.Log("Plus de rooms a generer restantes pour : " + lastRoomGenerated.name);
            yield break;
        }

        //Debug:
        //Debug.Log("Pour la room: " + lastRoomGenerated);
        //List<GameObject> debugList;
        //Debug.Log("Nombre de entrances libres: " + CountAvailableRooms(lastRoomGenerated, out debugList));
        //Debug.Log("Les entrances libres: " + debugList);

        //Si il y a au moins une entrance libre:
        if(CountAvailableEntrances(lastRoomGenerated) > 0)
        {
            roomsLeftToGenerate--;
            StartCoroutine(Generate(lastRoomGenerated));
        }

    }

    private Quaternion RotateQuaternion(Quaternion original, float x, float y, float z)
    {
        // Convertir l'angle d'Euler en quaternion
        Quaternion rotation = Quaternion.Euler(x, y, z);

        // Appliquer la rotation au quaternion d'origine
        return rotation * original;
    }

    private bool IsFacingAnotherRoom(GameObject parentRoom, GameObject entranceObject)
    {
        Vector3 origin = parentRoom.transform.TransformVector(entranceObject.transform.localPosition)
            + entranceObject.transform.right * checkDistanceForOtherRoom + parentRoom.transform.position;
        float radius = 1.0f;

        sphereGizmosOrigin = origin;

        //Debug.Log("Checking sphere at: " + origin);


        if (Physics.CheckSphere(origin, radius))
        {
            //isFacingAnotherRoom = true;
            return true;
        }
        //isFacingAnotherRoom = false;
        return false;
    }

    private uint CountAvailableEntrances(GameObject roomToCheck, out List<GameObject> freeEntrances)
    {
        freeEntrances = new List<GameObject>();
        uint counter = 0;

        foreach(GameObject entrance in roomToCheck.GetComponent<RoomInfoScript>().Entrances)
        {
            if(!IsFacingAnotherRoom(roomToCheck, entrance))
            {
                counter++;
                freeEntrances.Add(entrance);
            }
        }

        return counter;
    }

    private uint CountAvailableEntrances(GameObject roomToCheck)
    {
        uint counter = 0;

        foreach (GameObject entrance in roomToCheck.GetComponent<RoomInfoScript>().Entrances)
        {
            if (!IsFacingAnotherRoom(roomToCheck, entrance))
            {
                counter++;
            }
        }

        return counter;
    }

    private IEnumerator FillInWalls(List<GameObject> roomsToFill)
    {
        List<GameObject> freeEntrances;

        //Pour chaque room instantiee:
        foreach(GameObject room in roomsToFill)
        {
           //Si il reste encore des entrances libres dans cette room:
           if(CountAvailableEntrances(room, out freeEntrances) > 0)
            {
                //Pour chaque entree libre, on met des walls:
                foreach (GameObject entrance in freeEntrances)
                {
                    Vector3 wallPos = entrance.transform.position + wallOffset;
                    Quaternion wallRot = RotateQuaternion(entrance.transform.rotation, 0, 90, 0);
                    Instantiate(wallPrefab, wallPos, wallRot);
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
    }

    private IEnumerator GeneralCoroutine()
    {
        yield return StartCoroutine(Generate(this.gameObject));
        Debug.Log("Generation du niveau fini");
        
    }

    //private List<GameObject> ChooseRoomType()
    //{
    //    // Générer un nombre aléatoire entre 0 et 100
    //    float randomNumber = Random.Range(0f, 100f);
    //
    //    // Choisir le type de salle en fonction du nombre aléatoire
    //    if (randomNumber < oneWaySpawnProbability)
    //    {
    //        Debug.Log("1 way spawned");
    //        return oneWayRooms;
    //    }
    //    else if (randomNumber < oneWaySpawnProbability + twoWaySpawnProbability)
    //    {
    //        Debug.Log("2 way spawned");
    //        return twoWayRooms;
    //    }
    //    else if (randomNumber < oneWaySpawnProbability + twoWaySpawnProbability + threeWaySpawnProbability)
    //    {
    //        Debug.Log("3 way spawned");
    //        return threeWayRooms;
    //    }
    //    else
    //    {
    //        Debug.Log("4 way spawned");
    //        return fourWayRooms;
    //    }
    //
    //
    //    throw new System.Exception("Probleme de proba");
    //}

    //private bool ValidateProbabilities()
    //{
    //    if (oneWaySpawnProbability + twoWaySpawnProbability + threeWaySpawnProbability + fourWaySpawnProbability == 100)
    //        return true;
    //    else return false;
    //}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(sphereGizmosOrigin, 1.0f);
    }

}