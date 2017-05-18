/*
 * Copyright 2011 WorldWide Conferencing, LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package fortuneteller.neural.calibration

import scala.xml._
import util._

case class CalibratedIndicator(name: String,
  version: Int,
  normId: String,
  maxVal: Double,
  minVal: Double,
  normMax: Double,
  normMin: Double) {

}

object CalibratedIndicator {

  /**
   * Convert an item to XML
   */
  implicit def toXml(item: CalibratedIndicator): Node =
    <calibration name={ item.name } version={ item.version.toString } normId={ item.normId } maxVal={ item.maxVal.toString } minVal={ item.minVal.toString } normMax={ item.normMax.toString } normMin={ item.normMin.toString }></calibration>

  implicit def toXml(items: Seq[CalibratedIndicator]): Node = {
    <calibrationValues>{
      items.map(toXml)
    }</calibrationValues>
  }

  def fromXML(node: scala.xml.Node): CalibratedIndicator = {
    val name = (node \ "@name").text
    val version =  (node \ "@version").text.toInt
    val normId = (node \ "@normId").text
    val maxVal = (node \ "@maxVal").text.toDouble
    val minVal = (node \ "@minVal").text.toDouble
    val normMax = (node \ "@normMax").text.toDouble
    val normMin = (node \ "@normMin").text.toDouble
    new CalibratedIndicator(name, version, normId, maxVal, minVal, normMax, normMin)
  }
  def fromXMLDoc(xmlDoc: String): Seq[CalibratedIndicator] = {
    val indieList = new scala.collection.mutable.ListBuffer[CalibratedIndicator]();
    val xmlData = XML.loadString(xmlDoc)
    for ( entry <- xmlData \\ "calibration") {
      indieList.append(fromXML(entry))
    }
    indieList.toSeq
  }
}
